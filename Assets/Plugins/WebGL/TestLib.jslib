var MyPlugin = {
    Hello: function () {
        window.alert("Hello, world!");
    },
    HelloString: function (str) {
        window.alert(Pointer_stringify(str));
    },
    PrintFloatArray: function (array, size) {
        for (var i = 0; i < size; i++)
            console.log(HEAPF32[(array >> 2) + size]);
    },
    AddNumbers: function (x, y) {
        return x + y;
    },
    StringReturnValueFunction: function () {
        var returnStr = "bla";
        var buffer = _malloc(lengthBytesUTF8(returnStr) + 1);
        writeStringToMemory(returnStr, buffer);
        return buffer;
    },
    BindWebGLTexture: function (texture) {
        GLctx.bindTexture(GLctx.TEXTURE_2D, GL.textures[texture]);
    },

    UpdateGistID: function (id)
    {
        var str = Pointer_stringify(id);
    	//document.getElementById("gistid").value = str;
        window.location.assign(window.location.href.split("?")[0] + "?id=" + str);
    },

    GetGistID: function()
    {
		var returnStr = document.getElementById("gistid").value;
        var buffer = _malloc(lengthBytesUTF8(returnStr) + 1);
        writeStringToMemory(returnStr, buffer);
        return buffer;
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