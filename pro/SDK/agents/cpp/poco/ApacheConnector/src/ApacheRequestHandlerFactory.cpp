//
// ApacheRequestHandlerFactory.cpp
//
// $Id: //poco/1.4/ApacheConnector/src/ApacheRequestHandlerFactory.cpp#2 $
//
// Copyright (c) 2006-2011, Applied Informatics Software Engineering GmbH.
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


#include "ApacheRequestHandlerFactory.h"
#include "ApacheConnector.h"
#include "Poco/Net/HTTPRequestHandler.h"
#include "Poco/StringTokenizer.h"
#include "Poco/Manifest.h"
#include "Poco/File.h"
#include <vector>


using Poco::StringTokenizer;
using Poco::FastMutex;


ApacheRequestHandlerFactory::ApacheRequestHandlerFactory()
{
}


ApacheRequestHandlerFactory::~ApacheRequestHandlerFactory()
{
}


Poco::Net::HTTPRequestHandler* ApacheRequestHandlerFactory::createRequestHandler(const Poco::Net::HTTPServerRequest& request)
{
	FastMutex::ScopedLock lock(_mutex);
	
	// only if the given uri is found in _uris we are
	// handling this request.
	RequestHandlerFactories::iterator it = _requestHandlers.begin();
	RequestHandlerFactories::iterator itEnd = _requestHandlers.end();
	std::string uri = request.getURI();

	// if any uri in our map is found at the beginning of the given
	// uri -> then we handle it!!
	for (; it != itEnd; it++)
	{
		if (uri.find(it->first) == 0 || it->first.find(uri) == 0)
		{
			return it->second->createRequestHandler(request);
		}
	}

	return 0;
}


void ApacheRequestHandlerFactory::handleURIs(const std::string& uris)
{
	FastMutex::ScopedLock lock(_mutex);

	StringTokenizer st(uris, " ", StringTokenizer::TOK_TRIM);
	StringTokenizer::Iterator it = st.begin();
	StringTokenizer::Iterator itEnd = st.end();
	std::string factoryName = (*it);
	it++;
	std::string dllName = (*it);
	it++;

	for (; it != itEnd; it++)
	{
		addRequestHandlerFactory(dllName, factoryName, *it);
	}
}


void ApacheRequestHandlerFactory::addRequestHandlerFactory(const std::string& dllPath, const std::string& factoryName, const std::string& uri)
{	
	try
	{
		_loader.loadLibrary(dllPath);
		Poco::Net::HTTPRequestHandlerFactory* pFactory = _loader.classFor(factoryName).create();
		_requestHandlers.insert(std::make_pair(uri, pFactory));
	}
	catch (Poco::Exception& exc)
	{
		ApacheConnector::log(__FILE__, __LINE__, ApacheConnector::PRIO_ERROR, 0, exc.displayText().c_str());
	}
}


bool ApacheRequestHandlerFactory::mustHandle(const std::string& uri)
{
	FastMutex::ScopedLock lock(_mutex);

	// only if the given uri is found in _uris we are
	// handling this request.
	RequestHandlerFactories::iterator it = _requestHandlers.begin();
	RequestHandlerFactories::iterator itEnd = _requestHandlers.end();

	// if any uri in our map is found at the beginning of the given
	// uri -> then we handle it!!
	for (; it != itEnd; it++)
	{
		// dealing with both cases:
		// handler is registered with: /download
		// uri: /download/xyz
		// uri: /download
		if (uri.find(it->first) == 0 || it->first.find(uri) == 0)
			return true;
	}

	return false;
}
