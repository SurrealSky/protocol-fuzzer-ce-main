//
// UnicodeConverterTest.h
//
// $Id: //poco/1.4/Foundation/testsuite/src/UnicodeConverterTest.h#1 $
//
// Definition of the UnicodeConverterTest class.
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


#ifndef UnicodeConverterTest_INCLUDED
#define UnicodeConverterTest_INCLUDED


#include "Poco/Foundation.h"
#include "CppUnit/TestCase.h"
#include "Poco/UnicodeConverter.h"
#include <cstring>


class UnicodeConverterTest: public CppUnit::TestCase
{
public:
	UnicodeConverterTest(const std::string& name);
	~UnicodeConverterTest();

	void testUTF16();
	void testUTF32();

	void setUp();
	void tearDown();

	static CppUnit::Test* suite();

private:
	template <typename T>
	void runTests()
	{
		const unsigned char supp[] = {0x41, 0x42, 0xf0, 0x90, 0x82, 0xa4, 0xf0, 0xaf, 0xa6, 0xa0, 0xf0, 0xaf, 0xa8, 0x9d, 0x00};
		std::string text((const char*) supp);

		// Convert from UTF-8 to wide
		T wtext, wtext2, wtext3;
		Poco::UnicodeConverter::convert(text, wtext);
		Poco::UnicodeConverter::convert((const char*) supp, strlen((const char*) supp), wtext2);
		Poco::UnicodeConverter::convert((const char*) supp, wtext3);

		std::string text2, text3, text4;
	
		assert (text != text2);
		assert (text != text3);
		assert (text != text4);

		// Convert from wide to UTF-8
		Poco::UnicodeConverter::convert(wtext, text2);
		Poco::UnicodeConverter::convert(wtext2, text3);
		Poco::UnicodeConverter::convert(wtext3, text4);

		assert (text == text2);
		assert (text == text3);
		assert (text == text4);
	}
};


#endif // UnicodeConverterTest_INCLUDED
