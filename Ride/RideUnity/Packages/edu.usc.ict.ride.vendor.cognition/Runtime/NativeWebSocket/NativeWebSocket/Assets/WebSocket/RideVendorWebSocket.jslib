
var RideVendorLibraryWebSocket = {
	$RideVendorwebSocketState: {
		/*
		 * Map of instances
		 *
		 * Instance structure:
		 * {
		 * 	url: string,
		 * 	ws: WebSocket
		 * }
		 */
		instances: {},

		/* Last instance ID */
		lastId: 0,

		/* Event listeners */
		onOpen: null,
		onMesssage: null,
		onError: null,
		onClose: null,

		/* Debug mode */
		debug: false,

		wasm64: false,

		ptrToOffset: function(ptr) {
			return typeof ptr === "bigint" ? Number(ptr) : ptr;
		},

		utf8ToString: function(ptr) {
			return UTF8ToString(RideVendorwebSocketState.ptrToOffset(ptr));
		},

		notePtr: function(ptr) {
			if (typeof ptr === "bigint")
				RideVendorwebSocketState.wasm64 = true;
		},

		ptrToAbi: function(ptr) {
			return RideVendorwebSocketState.wasm64 && typeof ptr !== "bigint" ? BigInt(ptr) : ptr;
		}
	},

	/**
	 * Set onOpen callback
	 *
	 * @param callback Reference to C# static function
	 */
	RideVendorWebSocketSetOnOpen: function(callback) {

		RideVendorwebSocketState.onOpen = callback;

	},

	/**
	 * Set onMessage callback
	 *
	 * @param callback Reference to C# static function
	 */
	RideVendorWebSocketSetOnMessage: function(callback) {

		RideVendorwebSocketState.onMessage = callback;

	},

	/**
	 * Set onError callback
	 *
	 * @param callback Reference to C# static function
	 */
	RideVendorWebSocketSetOnError: function(callback) {

		RideVendorwebSocketState.onError = callback;

	},

	/**
	 * Set onClose callback
	 *
	 * @param callback Reference to C# static function
	 */
	RideVendorWebSocketSetOnClose: function(callback) {

		RideVendorwebSocketState.onClose = callback;

	},

	/**
	 * Allocate new WebSocket instance struct
	 *
	 * @param url Server URL
	 */
	RideVendorWebSocketAllocate: function(url) {

		RideVendorwebSocketState.notePtr(url);
		var urlStr = RideVendorwebSocketState.utf8ToString(url);
		var id = RideVendorwebSocketState.lastId++;

		RideVendorwebSocketState.instances[id] = {
		  subprotocols: [],
			url: urlStr,
			ws: null
		};

		return id;

	},

  /**
   * Add subprotocol to instance
   *
   * @param instanceId Instance ID
   * @param subprotocol Subprotocol name to add to instance
   */
  RideVendorWebSocketAddSubProtocol: function(instanceId, subprotocol) {

    RideVendorwebSocketState.notePtr(subprotocol);
    var subprotocolStr = RideVendorwebSocketState.utf8ToString(subprotocol);
    RideVendorwebSocketState.instances[instanceId].subprotocols.push(subprotocolStr);

  },

	/**
	 * Remove reference to WebSocket instance
	 *
	 * If socket is not closed function will close it but onClose event will not be emitted because
	 * this function should be invoked by C# WebSocket destructor.
	 *
	 * @param instanceId Instance ID
	 */
	RideVendorWebSocketFree: function(instanceId) {

		var instance = RideVendorwebSocketState.instances[instanceId];

		if (!instance) return 0;

		// Close if not closed
		if (instance.ws && instance.ws.readyState < 2)
			instance.ws.close();

		// Remove reference
		delete RideVendorwebSocketState.instances[instanceId];

		return 0;

	},

	/**
	 * Connect WebSocket to the server
	 *
	 * @param instanceId Instance ID
	 */
	RideVendorWebSocketConnect: function(instanceId) {

		var instance = RideVendorwebSocketState.instances[instanceId];
		if (!instance) return -1;

		if (instance.ws !== null)
			return -2;

		instance.ws = new WebSocket(instance.url, instance.subprotocols);

		instance.ws.binaryType = 'arraybuffer';

		instance.ws.onopen = function() {

			if (RideVendorwebSocketState.debug)
				console.log("[JSLIB WebSocket] Connected.");

			if (RideVendorwebSocketState.onOpen) {
				var callback = RideVendorwebSocketState.onOpen;
				{{{ makeDynCall('vi', 'callback') }}}(instanceId);
			}

		};

		instance.ws.onmessage = function(ev) {

			if (RideVendorwebSocketState.debug)
				console.log("[JSLIB WebSocket] Received message:", ev.data);

			if (RideVendorwebSocketState.onMessage === null)
				return;

			if (ev.data instanceof ArrayBuffer) {

				var dataBuffer = new Uint8Array(ev.data);

				var buffer = _malloc(dataBuffer.length);
				HEAPU8.set(dataBuffer, RideVendorwebSocketState.ptrToOffset(buffer));

				try {
					var callback = RideVendorwebSocketState.onMessage;
					{{{ makeDynCall('viii', 'callback') }}}(instanceId, RideVendorwebSocketState.ptrToAbi(buffer), dataBuffer.length);
				} finally {
					_free(buffer);
				}

      } else {
				var dataBuffer = (new TextEncoder()).encode(ev.data);

				var buffer = _malloc(dataBuffer.length);
				HEAPU8.set(dataBuffer, RideVendorwebSocketState.ptrToOffset(buffer));

				try {
					var callback = RideVendorwebSocketState.onMessage;
					{{{ makeDynCall('viii', 'callback') }}}(instanceId, RideVendorwebSocketState.ptrToAbi(buffer), dataBuffer.length);
				} finally {
					_free(buffer);
				}

      }

		};

		instance.ws.onerror = function(ev) {

			if (RideVendorwebSocketState.debug)
				console.log("[JSLIB WebSocket] Error occured.");

			if (RideVendorwebSocketState.onError) {

				var msg = "WebSocket error.";
				var length = lengthBytesUTF8(msg) + 1;
				var buffer = _malloc(length);
				stringToUTF8(msg, RideVendorwebSocketState.ptrToOffset(buffer), length);

				try {
					var callback = RideVendorwebSocketState.onError;
					{{{ makeDynCall('vii', 'callback') }}}(instanceId, RideVendorwebSocketState.ptrToAbi(buffer));
				} finally {
					_free(buffer);
				}

			}

		};

		instance.ws.onclose = function(ev) {

			if (RideVendorwebSocketState.debug)
				console.log("[JSLIB WebSocket] Closed.");

			if (RideVendorwebSocketState.onClose) {
				var callback = RideVendorwebSocketState.onClose;
				{{{ makeDynCall('vii', 'callback') }}}(instanceId, ev.code);
			}

			delete instance.ws;

		};

		return 0;

	},

	/**
	 * Close WebSocket connection
	 *
	 * @param instanceId Instance ID
	 * @param code Close status code
	 * @param reasonPtr Pointer to reason string
	 */
	RideVendorWebSocketClose: function(instanceId, code, reasonPtr) {

		RideVendorwebSocketState.notePtr(reasonPtr);
		var instance = RideVendorwebSocketState.instances[instanceId];
		if (!instance) return -1;

		if (!instance.ws)
			return -3;

		if (instance.ws.readyState === 2)
			return -4;

		if (instance.ws.readyState === 3)
			return -5;

		var reason = ( reasonPtr ? RideVendorwebSocketState.utf8ToString(reasonPtr) : undefined );

		try {
			instance.ws.close(code, reason);
		} catch(err) {
			return -7;
		}

		return 0;

	},

	/**
	 * Send message over WebSocket
	 *
	 * @param instanceId Instance ID
	 * @param bufferPtr Pointer to the message buffer
	 * @param length Length of the message in the buffer
	 */
	RideVendorWebSocketSend: function(instanceId, bufferPtr, length) {

		RideVendorwebSocketState.notePtr(bufferPtr);
		var instance = RideVendorwebSocketState.instances[instanceId];
		if (!instance) return -1;

		if (!instance.ws)
			return -3;

		if (instance.ws.readyState !== 1)
			return -6;

		var bufferOffset = RideVendorwebSocketState.ptrToOffset(bufferPtr);
		instance.ws.send(HEAPU8.buffer.slice(bufferOffset, bufferOffset + length));

		return 0;

	},

	/**
	 * Send text message over WebSocket
	 *
	 * @param instanceId Instance ID
	 * @param bufferPtr Pointer to the message buffer
	 * @param length Length of the message in the buffer
	 */
	RideVendorWebSocketSendText: function(instanceId, message) {

		RideVendorwebSocketState.notePtr(message);
		var instance = RideVendorwebSocketState.instances[instanceId];
		if (!instance) return -1;

		if (!instance.ws)
			return -3;

		if (instance.ws.readyState !== 1)
			return -6;

		instance.ws.send(RideVendorwebSocketState.utf8ToString(message));

		return 0;

	},

	/**
	 * Return WebSocket readyState
	 *
	 * @param instanceId Instance ID
	 */
	RideVendorWebSocketGetState: function(instanceId) {

		var instance = RideVendorwebSocketState.instances[instanceId];
		if (!instance) return -1;

		if (instance.ws)
			return instance.ws.readyState;
		else
			return 3;

	}

};

autoAddDeps(RideVendorLibraryWebSocket, '$RideVendorwebSocketState');
mergeInto(LibraryManager.library, RideVendorLibraryWebSocket);
