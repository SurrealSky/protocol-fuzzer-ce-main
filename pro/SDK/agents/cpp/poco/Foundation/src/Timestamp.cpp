//
// Timestamp.cpp
//
// $Id: //poco/1.4/Foundation/src/Timestamp.cpp#2 $
//
// Library: Foundation
// Package: DateTime
// Module:  Timestamp
//
// Copyright (c) 2004-2006, Applied Informatics Software Engineering GmbH.
// and Contributors.
//
// Permission is hereby granted, free of charge, to any person or organization
// obtaining a copy of the software and accompanying documentation covered by
// this license (the "Software") to use, reproduce, display, distribute,
// execute, and transmit the Software, and to prepare derivative works of the
// Software, and to permit third-parties to whom the Software is furnished to
// do so, all subject to the following:
// 
// The copyright notices in the Software and this entire statement, including
// the above license grant, this restriction and the following disclaimer,
// must be included in all copies of the Software, in whole or in part, and
// all derivative works of the Software, unless such copies or derivative
// works are solely in the form of machine-executable object code generated by
// a source language processor.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE, TITLE AND NON-INFRINGEMENT. IN NO EVENT
// SHALL THE COPYRIGHT HOLDERS OR ANYONE DISTRIBUTING THE SOFTWARE BE LIABLE
// FOR ANY DAMAGES OR OTHER LIABILITY, WHETHER IN CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//


#include "Poco/Timestamp.h"
#include "Poco/Timespan.h"
#include "Poco/Exception.h"
#include <algorithm>
#if defined(POCO_OS_FAMILY_UNIX)
#include <time.h>
#include <unistd.h>
#if defined(POCO_VXWORKS)
#include <timers.h>
#else
#include <sys/time.h>
#include <sys/times.h>
#endif
#elif defined(POCO_OS_FAMILY_WINDOWS)
#include "Poco/UnWindows.h"
#if defined(_WIN32_WCE)
#include <cmath>
#endif
#endif


#if defined(_WIN32_WCE)


//
// See <http://community.opennetcf.com/articles/cf/archive/2007/11/20/getting-a-millisecond-resolution-datetime-under-windows-ce.aspx>
// for an explanation of the following code.
//
// In short: Windows CE system time in most cases only has a resolution of one second.
// But we want millisecond resolution.
//


namespace {


class TickOffset
{
public:
	TickOffset()
	{
		SYSTEMTIME st1, st2;
		std::memset(&st1, 0, sizeof(SYSTEMTIME));
		std::memset(&st2, 0, sizeof(SYSTEMTIME));
		GetSystemTime(&st1);
		while (true)
		{
			GetSystemTime(&st2);

			// wait for a rollover
			if (st1.wSecond != st2.wSecond)
			{
				_offset = GetTickCount() % 1000;
				break;
			}
		}
	}

	void calibrate(int seconds)
	{
		SYSTEMTIME st1, st2;
		systemTime(&st1);

		WORD s = st1.wSecond;
		int sum = 0;
		int remaining = seconds;
		while (remaining > 0)
		{
			systemTime(&st2);
			WORD s2 = st2.wSecond;

			if (s != s2)
			{
				remaining--;
				// store the offset from zero
				sum += (st2.wMilliseconds > 500) ? (st2.wMilliseconds - 1000) : st2.wMilliseconds;
				s = st2.wSecond;
			}
		}

		// adjust the offset by the average deviation from zero (round to the integer farthest from zero)
		if (sum < 0)
			_offset += (int) std::floor(sum / (float)seconds);
		else
			_offset += (int) std::ceil(sum / (float)seconds);
	}

	void systemTime(SYSTEMTIME* pST)
	{
		std::memset(pST, 0, sizeof(SYSTEMTIME));
		
		WORD tick = GetTickCount() % 1000;
		GetSystemTime(pST);
		WORD ms = (tick >= _offset) ? (tick - _offset) : (1000 - (_offset - tick));
		pST->wMilliseconds = ms;	
	}

	void systemTimeAsFileTime(FILETIME* pFT)
	{
		SYSTEMTIME st;
		systemTime(&st);
		SystemTimeToFileTime(&st, pFT);	
	}

private:
	WORD _offset;
};


static TickOffset offset;


void GetSystemTimeAsFileTimeWithMillisecondResolution(FILETIME* pFT)
{
	offset.systemTimeAsFileTime(pFT);	
}


} // namespace


#endif // defined(_WIN32_WCE)


namespace Poco {


Timestamp::Timestamp()
{
	update();
}


Timestamp::Timestamp(TimeVal tv)
{
	_ts = tv;
}


Timestamp::Timestamp(const Timestamp& other)
{
	_ts = other._ts;
}


Timestamp::~Timestamp()
{
}


Timestamp& Timestamp::operator = (const Timestamp& other)
{
	_ts = other._ts;
	return *this;
}


Timestamp& Timestamp::operator = (TimeVal tv)
{
	_ts = tv;
	return *this;
}


void Timestamp::swap(Timestamp& timestamp)
{
	std::swap(_ts, timestamp._ts);
}


Timestamp Timestamp::fromEpochTime(std::time_t t)
{
	return Timestamp(TimeVal(t)*resolution());
}


Timestamp Timestamp::fromUtcTime(UtcTimeVal val)
{
	val -= (TimeDiff(0x01b21dd2) << 32) + 0x13814000;
	val /= 10;
	return Timestamp(val);
}


void Timestamp::update()
{
#if defined(POCO_OS_FAMILY_WINDOWS)

	FILETIME ft;
#if defined(_WIN32_WCE)
	GetSystemTimeAsFileTimeWithMillisecondResolution(&ft);
#else
	GetSystemTimeAsFileTime(&ft);
#endif

	ULARGE_INTEGER epoch; // UNIX epoch (1970-01-01 00:00:00) expressed in Windows NT FILETIME
	epoch.LowPart  = 0xD53E8000;
	epoch.HighPart = 0x019DB1DE;

	ULARGE_INTEGER ts;
	ts.LowPart  = ft.dwLowDateTime;
	ts.HighPart = ft.dwHighDateTime;
	ts.QuadPart -= epoch.QuadPart;
	_ts = ts.QuadPart/10;

#elif defined(POCO_VXWORKS)

	struct timespec ts;
	if (clock_gettime(CLOCK_REALTIME, &ts))
		throw SystemException("cannot get time of day");
	_ts = TimeVal(ts.tv_sec)*resolution() + ts.tv_nsec/1000;

#else

	struct timeval tv;
	if (gettimeofday(&tv, NULL))
		throw SystemException("cannot get time of day");
	_ts = TimeVal(tv.tv_sec)*resolution() + tv.tv_usec;
	
#endif
}


Timestamp  Timestamp::operator +  (const Timespan& span) const
{
	return *this + span.totalMicroseconds();
}


Timestamp  Timestamp::operator -  (const Timespan& span) const
{
	return *this - span.totalMicroseconds();
}


Timestamp& Timestamp::operator += (const Timespan& span)
{
	return *this += span.totalMicroseconds();
}


Timestamp& Timestamp::operator -= (const Timespan& span)
{
	return *this -= span.totalMicroseconds();
}


#if defined(_WIN32)


Timestamp Timestamp::fromFileTimeNP(UInt32 fileTimeLow, UInt32 fileTimeHigh)
{
	ULARGE_INTEGER epoch; // UNIX epoch (1970-01-01 00:00:00) expressed in Windows NT FILETIME
	epoch.LowPart  = 0xD53E8000;
	epoch.HighPart = 0x019DB1DE;
	
	ULARGE_INTEGER ts;
	ts.LowPart  = fileTimeLow;
	ts.HighPart = fileTimeHigh;
	ts.QuadPart -= epoch.QuadPart;

	return Timestamp(ts.QuadPart/10);
}


void Timestamp::toFileTimeNP(UInt32& fileTimeLow, UInt32& fileTimeHigh) const
{
	ULARGE_INTEGER epoch; // UNIX epoch (1970-01-01 00:00:00) expressed in Windows NT FILETIME
	epoch.LowPart  = 0xD53E8000;
	epoch.HighPart = 0x019DB1DE;

	ULARGE_INTEGER ts;
	ts.QuadPart  = _ts*10;
	ts.QuadPart += epoch.QuadPart;
	fileTimeLow  = ts.LowPart;
	fileTimeHigh = ts.HighPart;
}


#endif


} // namespace Poco
