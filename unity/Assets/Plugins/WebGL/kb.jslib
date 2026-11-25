mergeInto(LibraryManager.library, {
  KB_Emit: function (ptr) {
    var s = UTF8ToString(ptr);
    try { if (window.KB && window.KB.onUnity) window.KB.onUnity(s); } catch (e) { console.error(e); }
  }
});
