mergeInto(LibraryManager.library, {
  ReactDispatchString: function (eventName, str) {
    var eventNameStr = UTF8ToString(eventName);
    var strVal = UTF8ToString(str);
    window.dispatchReactUnityEvent(eventNameStr, strVal);
  }
});
