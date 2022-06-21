//
// HTTPAuthenticationParams.cpp
//
// $Id: //poco/1.4/Net/src/HTTPAuthenticationParams.cpp#1 $
//
// Library: Net
// Package: HTTP
// Module:	HTTPAuthenticationParams
//
// Copyright (c) 2011, Anton V. Yabchinskiy (arn at bestmx dot ru).
// Copyright (c) 2012, Applied Informatics Software Engineering GmbH.
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


#include "Poco/Exception.h"
#include "Poco/Net/HTTPAuthenticationParams.h"
#include "Poco/Net/HTTPRequest.h"
#include "Poco/Net/HTTPResponse.h"
#include "Poco/Net/NetException.h"
#include "Poco/String.h"
#include "Poco/Ascii.h"


using Poco::icompare;
using Poco::Ascii;


namespace 
{
	bool mustBeQuoted(const std::string& name)
	{
		return
			icompare(name, "cnonce") == 0 ||
			icompare(name, "domain") == 0 ||
			icompare(name, "nonce") == 0 ||
			icompare(name, "opaque") == 0 ||
			icompare(name, "qop") == 0 ||
			icompare(name, "realm") == 0 ||
			icompare(name, "response") == 0 ||
			icompare(name, "uri") == 0 ||
			icompare(name, "username") == 0;
	}
	
	
	void formatParameter(std::string& result, const std::string& name, const std::string& value)
	{
		result += name;
		result += '=';
		if (mustBeQuoted(name)) 
		{
			result += '"';
			result += value;
			result += '"';
		} 
		else 
		{
			result += value;
		}
	}
}


namespace Poco {
namespace Net {


const std::string HTTPAuthenticationParams::REALM("realm");


HTTPAuthenticationParams::HTTPAuthenticationParams()
{
}


HTTPAuthenticationParams::HTTPAuthenticationParams(const std::string& authInfo)
{
	fromAuthInfo(authInfo);
}


HTTPAuthenticationParams::HTTPAuthenticationParams(const HTTPRequest& request)
{
	fromRequest(request);
}


HTTPAuthenticationParams::HTTPAuthenticationParams(const HTTPResponse& response)
{
	fromResponse(response);
}


HTTPAuthenticationParams::~HTTPAuthenticationParams()
{
}


HTTPAuthenticationParams& HTTPAuthenticationParams::operator = (const HTTPAuthenticationParams& authParams)
{
	NameValueCollection::operator = (authParams);

	return *this;
}


void HTTPAuthenticationParams::fromAuthInfo(const std::string& authInfo)
{
	parse(authInfo.begin(), authInfo.end());
}


void HTTPAuthenticationParams::fromRequest(const HTTPRequest& request)
{
	std::string scheme;
	std::string authInfo;

	request.getCredentials(scheme, authInfo);

	if (icompare(scheme, "Digest") != 0) 
		throw InvalidArgumentException("Could not parse non-Digest authentication information", scheme);

	fromAuthInfo(authInfo);
}


void HTTPAuthenticationParams::fromResponse(const HTTPResponse& response)
{
	if (!response.has("WWW-Authenticate")) 
		throw NotAuthenticatedException("HTTP response has no authentication header");

	const std::string& header = response.get("WWW-Authenticate");

	if (icompare(header, 0, 6, "Basic ") == 0) 
	{
		parse(header.begin() + 6, header.end());
	} 
	else if (icompare(header, 0, 7, "Digest ") == 0)
	{
		parse(header.begin() + 7, header.end());
	} 
	else throw InvalidArgumentException("Invalid authentication scheme", header);
}


const std::string& HTTPAuthenticationParams::getRealm() const
{
	return get(REALM);
}


void HTTPAuthenticationParams::setRealm(const std::string& realm)
{
	set(REALM, realm);
}


std::string HTTPAuthenticationParams::toString() const
{
	ConstIterator iter = begin();
	std::string result;

	if (iter != end()) 
	{
		formatParameter(result, iter->first, iter->second);
		++iter;
	}

	for (; iter != end(); ++iter) 
	{
		result.append(", ");
		formatParameter(result, iter->first, iter->second);
	}

	return result;
}


void HTTPAuthenticationParams::parse(std::string::const_iterator first, std::string::const_iterator last)
{
	enum State 
	{
		STATE_INITIAL = 0x0100,
		STATE_FINAL = 0x0200,

		STATE_SPACE = STATE_INITIAL | 0,
		STATE_TOKEN = 1,
		STATE_EQUALS = 2,
		STATE_VALUE = STATE_FINAL | 3,
		STATE_VALUE_QUOTED = 4,
		STATE_VALUE_ESCAPE = 5,
		STATE_COMMA = STATE_FINAL | 6
	};

	int state = STATE_SPACE;
	std::string token;
	std::string value;

	for (std::string::const_iterator it = first; it != last; ++it) 
	{
		switch (state) 
		{
		case STATE_SPACE:
			if (Ascii::isAlphaNumeric(*it)) 
			{
				token += *it;
				state = STATE_TOKEN;
			} 
			else if (Ascii::isSpace(*it)) 
			{
				// Skip
			} 
			else throw SyntaxException("Invalid authentication information");
			break;

		case STATE_TOKEN:
			if (*it == '=') 
			{
				state = STATE_EQUALS;
			} 
			else if (Ascii::isAlphaNumeric(*it)) 
			{
				token += *it;
			} 
			else throw SyntaxException("Invalid authentication information");
			break;

		case STATE_EQUALS:
			if (Ascii::isAlphaNumeric(*it)) 
			{
				value += *it;
				state = STATE_VALUE;
			} 
			else if (*it == '"') 
			{
				state = STATE_VALUE_QUOTED;
			} 
			else throw SyntaxException("Invalid authentication information");
			break;

		case STATE_VALUE_QUOTED:
			if (*it == '\\') 
			{
				state = STATE_VALUE_ESCAPE;
			} 
			else if (*it == '"') 
			{
				add(token, value);
				token.clear();
				value.clear();
				state = STATE_COMMA;
			} 
			else 
			{
				value += *it;
			}
			break;

		case STATE_VALUE_ESCAPE:
			value += *it;
			state = STATE_VALUE_QUOTED;
			break;

		case STATE_VALUE:
			if (Ascii::isSpace(*it)) 
			{
				add(token, value);
				token.clear();
				value.clear();
				state = STATE_COMMA;
			} 
			else if (*it == ',') 
			{
				add(token, value);
				token.clear();
				value.clear();
				state = STATE_SPACE;
			} 
			else 
			{
				value += *it;
			}
			break;

		case STATE_COMMA:
			if (*it == ',')
			{
				state = STATE_SPACE;
			} 
			else if (Ascii::isSpace(*it)) 
			{
				// Skip
			} 
			else throw SyntaxException("Invalid authentication information");
			break;
		}
	}

	if (!(state & STATE_FINAL))
		throw SyntaxException("Invalid authentication information");
}


} } // namespace Poco::Net
