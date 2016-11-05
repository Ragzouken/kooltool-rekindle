var MyPlugin = {
    UpdateGistID: function (id)
    {
        var str = Pointer_stringify(id);
    	//document.getElementById("gistid").value = str;
        window.location.assign(window.location.href.split("?")[0] + "?id=" + str);
    },

    GetWindowSearch: function()
    {
        var str = window.location.search.replace("?", "");
        var buffer = _malloc(lengthBytesUTF8(str) + 1);
        writeStringToMemory(str, buffer);
        return buffer;
    },
};

mergeInto(LibraryManager.library, MyPlugin);