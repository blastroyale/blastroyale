var Lib_BEST_HTTP_WebGL_HTTP_Bridge =
{
	/*LogLevels: {
		All: 0,
		Information: 1,
		Warning: 2,
		Error: 3,
		Exception: 4,
		None: 5
	}*/

	$_best_http_request_bridge_global: {
		requestInstances: {},
		nextRequestId: 1,
		loglevel: 2,

        SendTextToCSharpSide: function(request, onbuffer, text)
        {
            const encoder = new TextEncoder();
            const byteArray = encoder.encode(text);

            const array = Module['dynCall_iii'](_best_http_request_bridge_global.onallocbuffer, request, byteArray.length);

            HEAPU8.set(byteArray, array);

            Module['dynCall_viii'](onbuffer, request, array, byteArray.length);
        },

        GetResponseHeaders: function(request, callback)
	    {
		    if (_best_http_request_bridge_global.loglevel <= 1) /*information*/
			    console.log(`GetResponseHeaders(${request})`);

            var headers = ''
            var cookies = document.cookie.split(';');
            for(var i = 0; i < cookies.length; ++i) {
                const cookie = cookies[i].trim();

                if (cookie.length > 0)
                    headers += "Set-Cookie:" + cookie + "\n";
            }

            var additionalHeaders = _best_http_request_bridge_global.requestInstances[request].getAllResponseHeaders().trim();
            if (additionalHeaders.length > 0) {
                headers += additionalHeaders;
                headers += "\n";
            }

            //headers += "\n";

            _best_http_request_bridge_global.SendTextToCSharpSide(request, callback, headers);
	    },
	},    

	XHR_Create: function(method, url, user, passwd, withCredentials)
	{
		var _url = new URL(UTF8ToString(url));
		var _method = UTF8ToString(method);

		if (_best_http_request_bridge_global.loglevel <= 1) /*information*/
			console.log(`XHR_Create (${_best_http_request_bridge_global.nextRequestId}, ${_method}, ${_url.toString()}, ${withCredentials})`);

		var http = new XMLHttpRequest();

		if (user && passwd)
		{
			var u = UTF8ToString(user);
			var p = UTF8ToString(passwd);

            // https://developer.mozilla.org/en-US/docs/Web/API/XMLHttpRequest/withCredentials
			http.withCredentials = true;
			http.open(_method, _url.toString(), /*async:*/ true , u, p);
		}
		else {
            http.withCredentials = withCredentials;
			http.open(_method, _url.toString(), /*async:*/ true);
        }

		http.responseType = 'arraybuffer';

		_best_http_request_bridge_global.requestInstances[_best_http_request_bridge_global.nextRequestId] = http;
		return _best_http_request_bridge_global.nextRequestId++;
	},

	XHR_SetTimeout: function (request, timeout)
	{
		if (_best_http_request_bridge_global.loglevel <= 1) /*information*/
			console.log(`XHR_SetTimeout(${request}, ${timeout})`);

		_best_http_request_bridge_global.requestInstances[request].timeout = timeout;
	},

	XHR_SetRequestHeader: function (request, header, value)
	{
		var _header = UTF8ToString(header);
		var _value = UTF8ToString(value);

		if (_best_http_request_bridge_global.loglevel <= 1) /*information*/
			console.log(`XHR_SetRequestHeader(${_header}, ${_value})`);

        if (_header != 'Cookie')
		    _best_http_request_bridge_global.requestInstances[request].setRequestHeader(_header, _value);
        else {
            var cookies = _value.split(';');
            for (var i = 0; i < cookies.length; i++) {
                document.cookie = cookies[i];
            }
        }
	},

	XHR_SetResponseHandler: function (request, onresponse, onerror, ontimeout, onaborted, onbuffer, onallocbuffer)
	{
		if (_best_http_request_bridge_global.loglevel <= 1) /*information*/
			console.log(`XHR_SetResponseHandler(${request})`);

        _best_http_request_bridge_global.onallocbuffer = onallocbuffer;

		var http = _best_http_request_bridge_global.requestInstances[request];

        // https://developer.mozilla.org/en-US/docs/Web/API/XMLHttpRequest/readystatechange_event
        // The readystatechange event is fired whenever the readyState property of the XMLHttpRequest changes.
        // https://xhr.spec.whatwg.org/#dom-xmlhttprequest-readystate
        http.onreadystatechange = (event) => {

			if (_best_http_request_bridge_global.loglevel <= 1) /*information*/
				console.log(`${request} onreadystatechange(${http.readyState})`);

            switch (http.readyState){
                // The object has been constructed.
                case XMLHttpRequest.UNSENT: break;

                // The open() method has been successfully invoked. During this state request headers can be set using setRequestHeader() and the fetch can be initiated using the send() method.
                case XMLHttpRequest.OPENED: break;

                // All redirects (if any) have been followed and all headers of a response have been received.
                case XMLHttpRequest.HEADERS_RECEIVED: {
                    _best_http_request_bridge_global.SendTextToCSharpSide(request, onbuffer, `HTTP/1.1 ${http.status} ${http.statusText}\n`);

                    _best_http_request_bridge_global.GetResponseHeaders(request, onbuffer);
                    break;
                }

                // The response body is being received.
                case XMLHttpRequest.LOADING: break;

                // The data transfer has been completed or something went wrong during the transfer (e.g., infinite redirects).
                case XMLHttpRequest.DONE: break;
            }
        };

        http.onloadstart = (event) => {
            if (_best_http_request_bridge_global.loglevel <= 1) /*information*/
                console.log(`${request} onloadstart: ${event}`);
        };

        // https://developer.mozilla.org/en-US/docs/Web/API/XMLHttpRequest/load_event
		// Fired when an XMLHttpRequest transaction completes successfully. Also available via the onload event handler property.
		http.onload = function http_onload(e) {
			if (_best_http_request_bridge_global.loglevel <= 1) /*information*/
				console.log(`${request} onload(${http.status}, ${http.statusText})`);

			if (onresponse)
			{
				var responseLength = 0;
                var array = 0;

                const setLengthHeader = http.getResponseHeader("content-length") === null;
                if (!setLengthHeader)
                    _best_http_request_bridge_global.SendTextToCSharpSide(request, onbuffer, `\n`);

				if (!!http.response){
                    var response = http.response;
					responseLength = response.byteLength;

                    if (setLengthHeader)
                        _best_http_request_bridge_global.SendTextToCSharpSide(request, onbuffer, `content-length:${responseLength}\n\n`);

                    array = Module['dynCall_iii'](onallocbuffer, request, responseLength);

                    var responseBytes = new Uint8Array(response);
				    var buffer = HEAPU8.subarray(array, array + responseLength);
				    buffer.set(responseBytes)
                }
                else if (setLengthHeader) {
                    _best_http_request_bridge_global.SendTextToCSharpSide(request, onbuffer, `content-length:0\n\n`);
                }

				Module['dynCall_viii'](onresponse, request, array, responseLength);
			}
		};

		if (onerror)
		{
			http.onerror = function http_onerror(e) {
				function HandleError(err)
				{
					var length = lengthBytesUTF8(err) + 1;
					var buffer = _malloc(length);

					stringToUTF8Array(err, HEAPU8, buffer, length);

					Module['dynCall_vii'](onerror, request, buffer);

					_free(buffer);
				}

				if (e.error)
					HandleError(e.error);
				else
					HandleError("Unknown Error! Maybe a CORS porblem?");
			};
		}

		if (ontimeout)
			http.ontimeout = function http_onerror(e) {
				Module['dynCall_vi'](ontimeout, request);
			};

		if (onaborted)
			http.onabort = function http_onerror(e) {
				Module['dynCall_vi'](onaborted, request);
			};
	},

	XHR_SetProgressHandler: function (request, onprogress, onuploadprogress)
	{
		if (_best_http_request_bridge_global.loglevel <= 1) /*information*/
			console.log(`XHR_SetProgressHandler(${request})`);

		var http = _best_http_request_bridge_global.requestInstances[request];
		if (http)
		{
			if (onprogress)
				http.onprogress = function http_onprogress(e) {
					if (_best_http_request_bridge_global.loglevel <= 1) /*information*/
						console.log(`XHR_SetProgressHandler download(${request}, ${e.loaded}, ${e.total})`);

					if (e.lengthComputable)
						Module['dynCall_viii'](onprogress, request, e.loaded, e.total);
				};

			if (onuploadprogress)
				http.upload.addEventListener("progress", function http_onprogress(e) {
					if (_best_http_request_bridge_global.loglevel <= 1) /*information*/
						console.log(`XHR_SetProgressHandler upload(${request}, ${e.loaded}, ${e.total})`);

					if (e.lengthComputable)
						Module['dynCall_viii'](onuploadprogress, request, e.loaded, e.total);
				}, true);
		}
	},

	XHR_Send: function (request, ptr, length)
	{
		if (_best_http_request_bridge_global.loglevel <= 1) /*information*/
			console.log(`XHR_Send(${request}, ${ptr}, ${length})`);

		var http = _best_http_request_bridge_global.requestInstances[request];

		try {
			if (length > 0)
				http.send(HEAPU8.subarray(ptr, ptr+length));
			else
				http.send();
		}
		catch(e) {
			if (_best_http_request_bridge_global.loglevel <= 4) /*exception*/
				console.error(`XHR_Send(${request}): ${e.name} : ${e.message}`);
		}
	},

	XHR_Abort: function (request)
	{
		if (_best_http_request_bridge_global.loglevel <= 1) /*information*/
			console.log(`XHR_Abort(${request})`);

		_best_http_request_bridge_global.requestInstances[request].abort();
	},

	XHR_Release: function (request)
	{
		if (_best_http_request_bridge_global.loglevel <= 1) /*information*/
			console.log(`XHR_Release(${request})`);

		delete _best_http_request_bridge_global.requestInstances[request];
	},

	XHR_SetLoglevel: function (level)
	{
		_best_http_request_bridge_global.loglevel = level;
	}
};

autoAddDeps(Lib_BEST_HTTP_WebGL_HTTP_Bridge, '$_best_http_request_bridge_global');
mergeInto(LibraryManager.library, Lib_BEST_HTTP_WebGL_HTTP_Bridge);
