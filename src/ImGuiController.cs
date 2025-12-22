/*
 * Lone EFT DMA Radar
 * Brought to you by Lone (Lone DMA)
 * 
MIT License

Copyright (c) 2025 Lone DMA

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 *
*/

using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace LoneEftDmaRadar;

public sealed class ImGuiController : IDisposable
{
    private readonly GL _gl;
    private readonly IWindow _window;
    private readonly IInputContext _input;
    private readonly bool _ownsInputContext;

    private uint _vao;
    private uint _vbo;
    private uint _ebo;
    private uint _fontTexture;
    private uint _shaderProgram;

    private int _attribLocationTex;
    private int _attribLocationProjMtx;
    private uint _attribLocationVtxPos;
    private uint _attribLocationVtxUV;
    private uint _attribLocationVtxColor;

    private int _windowWidth;
    private int _windowHeight;

    private readonly List<char> _pressedChars = [];
    private readonly Key[] _allKeys = Enum.GetValues<Key>();

    /// <summary>
    /// Creates a new ImGuiController.
    /// </summary>
    /// <param name="gl">OpenGL context.</param>
    /// <param name="window">Window to render to.</param>
    /// <param name="width">Initial window width.</param>
    /// <param name="height">Initial window height.</param>
    /// <param name="inputContext">Optional existing input context. If null, a new one will be created.</param>
    public ImGuiController(GL gl, IWindow window, int width, int height, IInputContext inputContext = null)
    {
        _gl = gl;
        _window = window;
        _windowWidth = width;
        _windowHeight = height;

        // Use provided input context or create a new one
        if (inputContext is not null)
        {
            _input = inputContext;
            _ownsInputContext = false;
        }
        else
        {
            _input = window.CreateInput();
            _ownsInputContext = true;
        }

        var io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

        CreateDeviceResources();
        SetupInput();

        io.Fonts.AddFontDefault();
        RecreateFontDeviceTexture();
    }

    private void SetupInput()
    {
        foreach (var keyboard in _input.Keyboards)
        {
            keyboard.KeyDown += OnKeyDown;
            keyboard.KeyUp += OnKeyUp;
            keyboard.KeyChar += OnKeyChar;
        }

        foreach (var mouse in _input.Mice)
        {
            mouse.MouseMove += OnMouseMove;
            mouse.MouseDown += OnMouseDown;
            mouse.MouseUp += OnMouseUp;
            mouse.Scroll += OnScroll;
        }
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        var io = ImGui.GetIO();
        var imKey = TranslateKey(key);
        if (imKey != ImGuiKey.None)
            io.AddKeyEvent(imKey, true);

        UpdateModifiers(keyboard, io);
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int scancode)
    {
        var io = ImGui.GetIO();
        var imKey = TranslateKey(key);
        if (imKey != ImGuiKey.None)
            io.AddKeyEvent(imKey, false);

        UpdateModifiers(keyboard, io);
    }

    private static void UpdateModifiers(IKeyboard keyboard, ImGuiIOPtr io)
    {
        io.AddKeyEvent(ImGuiKey.ModCtrl, keyboard.IsKeyPressed(Key.ControlLeft) || keyboard.IsKeyPressed(Key.ControlRight));
        io.AddKeyEvent(ImGuiKey.ModShift, keyboard.IsKeyPressed(Key.ShiftLeft) || keyboard.IsKeyPressed(Key.ShiftRight));
        io.AddKeyEvent(ImGuiKey.ModAlt, keyboard.IsKeyPressed(Key.AltLeft) || keyboard.IsKeyPressed(Key.AltRight));
        io.AddKeyEvent(ImGuiKey.ModSuper, keyboard.IsKeyPressed(Key.SuperLeft) || keyboard.IsKeyPressed(Key.SuperRight));
    }

    private void OnKeyChar(IKeyboard keyboard, char character)
    {
        _pressedChars.Add(character);
    }

    private void OnMouseMove(IMouse mouse, Vector2 position)
    {
        var io = ImGui.GetIO();
        io.AddMousePosEvent(position.X, position.Y);
    }

    private void OnMouseDown(IMouse mouse, MouseButton button)
    {
        var io = ImGui.GetIO();
        io.AddMouseButtonEvent((int)button, true);
    }

    private void OnMouseUp(IMouse mouse, MouseButton button)
    {
        var io = ImGui.GetIO();
        io.AddMouseButtonEvent((int)button, false);
    }

    private void OnScroll(IMouse mouse, ScrollWheel wheel)
    {
        var io = ImGui.GetIO();
        io.AddMouseWheelEvent(wheel.X, wheel.Y);
    }

    public void Update(float deltaTime)
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new Vector2(_windowWidth, _windowHeight);
        io.DeltaTime = deltaTime > 0 ? deltaTime : 1f / 60f;

        foreach (var c in _pressedChars)
            io.AddInputCharacter(c);
        _pressedChars.Clear();

        ImGui.NewFrame();
    }

    public void WindowResized(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }

    public void Render()
    {
        ImGui.Render();
        RenderDrawData(ImGui.GetDrawData());
    }

    private void CreateDeviceResources()
    {
        _vao = _gl.GenVertexArray();
        _gl.BindVertexArray(_vao);

        _vbo = _gl.GenBuffer();
        _ebo = _gl.GenBuffer();

        const string vertexShaderSource = """
            #version 330 core
            layout (location = 0) in vec2 Position;
            layout (location = 1) in vec2 UV;
            layout (location = 2) in vec4 Color;
            uniform mat4 ProjMtx;
            out vec2 Frag_UV;
            out vec4 Frag_Color;
            void main()
            {
                Frag_UV = UV;
                Frag_Color = Color;
                gl_Position = ProjMtx * vec4(Position.xy, 0, 1);
            }
            """;

        const string fragmentShaderSource = """
            #version 330 core
            in vec2 Frag_UV;
            in vec4 Frag_Color;
            uniform sampler2D Texture;
            layout (location = 0) out vec4 Out_Color;
            void main()
            {
                Out_Color = Frag_Color * texture(Texture, Frag_UV.st);
            }
            """;

        var vertexShader = CompileShader(ShaderType.VertexShader, vertexShaderSource);
        var fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentShaderSource);

        _shaderProgram = _gl.CreateProgram();
        _gl.AttachShader(_shaderProgram, vertexShader);
        _gl.AttachShader(_shaderProgram, fragmentShader);
        _gl.LinkProgram(_shaderProgram);

        _gl.GetProgram(_shaderProgram, ProgramPropertyARB.LinkStatus, out var linkStatus);
        if (linkStatus == 0)
        {
            var infoLog = _gl.GetProgramInfoLog(_shaderProgram);
            throw new Exception($"Error linking shader program: {infoLog}");
        }

        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);

        _attribLocationTex = _gl.GetUniformLocation(_shaderProgram, "Texture");
        _attribLocationProjMtx = _gl.GetUniformLocation(_shaderProgram, "ProjMtx");
        _attribLocationVtxPos = (uint)_gl.GetAttribLocation(_shaderProgram, "Position");
        _attribLocationVtxUV = (uint)_gl.GetAttribLocation(_shaderProgram, "UV");
        _attribLocationVtxColor = (uint)_gl.GetAttribLocation(_shaderProgram, "Color");

        _gl.BindVertexArray(0);
    }

    private uint CompileShader(ShaderType type, string source)
    {
        var shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out var compileStatus);
        if (compileStatus == 0)
        {
            var infoLog = _gl.GetShaderInfoLog(shader);
            throw new Exception($"Error compiling {type}: {infoLog}");
        }

        return shader;
    }

    private unsafe void RecreateFontDeviceTexture()
    {
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out nint pixels, out int width, out int height, out int _);

        _fontTexture = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, _fontTexture);
        _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (void*)pixels);

        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        io.Fonts.SetTexID((nint)_fontTexture);
    }

    private unsafe void RenderDrawData(ImDrawDataPtr drawData)
    {
        if (drawData.CmdListsCount == 0)
            return;

        // Backup GL state
        _gl.GetInteger(GetPName.ActiveTexture, out int lastActiveTexture);
        _gl.GetInteger(GetPName.CurrentProgram, out int lastProgram);
        _gl.GetInteger(GetPName.TextureBinding2D, out int lastTexture);
        _gl.GetInteger(GetPName.ArrayBufferBinding, out int lastArrayBuffer);
        _gl.GetInteger(GetPName.VertexArrayBinding, out int lastVertexArrayObject);
        Span<int> lastViewport = stackalloc int[4];
        _gl.GetInteger(GetPName.Viewport, lastViewport);
        Span<int> lastScissorBox = stackalloc int[4];
        _gl.GetInteger(GetPName.ScissorBox, lastScissorBox);
        _gl.GetInteger(GetPName.BlendSrcRgb, out int lastBlendSrcRgb);
        _gl.GetInteger(GetPName.BlendDstRgb, out int lastBlendDstRgb);
        _gl.GetInteger(GetPName.BlendSrcAlpha, out int lastBlendSrcAlpha);
        _gl.GetInteger(GetPName.BlendDstAlpha, out int lastBlendDstAlpha);
        _gl.GetInteger(GetPName.BlendEquationRgb, out int lastBlendEquationRgb);
        _gl.GetInteger(GetPName.BlendEquationAlpha, out int lastBlendEquationAlpha);
        var lastEnableBlend = _gl.IsEnabled(EnableCap.Blend);
        var lastEnableCullFace = _gl.IsEnabled(EnableCap.CullFace);
        var lastEnableDepthTest = _gl.IsEnabled(EnableCap.DepthTest);
        var lastEnableStencilTest = _gl.IsEnabled(EnableCap.StencilTest);
        var lastEnableScissorTest = _gl.IsEnabled(EnableCap.ScissorTest);

        // Setup render state
        // IMPORTANT: Activate texture unit 0 FIRST before any texture operations
        // Skia may have left a different texture unit active
        _gl.ActiveTexture(TextureUnit.Texture0);

        _gl.Enable(EnableCap.Blend);
        _gl.BlendEquation(BlendEquationModeEXT.FuncAdd);
        _gl.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
        _gl.Disable(EnableCap.CullFace);
        _gl.Disable(EnableCap.DepthTest);
        _gl.Disable(EnableCap.StencilTest);
        _gl.Enable(EnableCap.ScissorTest);

        // Setup orthographic projection matrix
        float L = drawData.DisplayPos.X;
        float R = drawData.DisplayPos.X + drawData.DisplaySize.X;
        float T = drawData.DisplayPos.Y;
        float B = drawData.DisplayPos.Y + drawData.DisplaySize.Y;

        Span<float> orthoProjection =
        [
            2.0f / (R - L), 0.0f, 0.0f, 0.0f,
            0.0f, 2.0f / (T - B), 0.0f, 0.0f,
            0.0f, 0.0f, -1.0f, 0.0f,
            (R + L) / (L - R), (T + B) / (B - T), 0.0f, 1.0f
        ];

        _gl.UseProgram(_shaderProgram);
        _gl.Uniform1(_attribLocationTex, 0);
        _gl.UniformMatrix4(_attribLocationProjMtx, 1, false, orthoProjection);

        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);

        // Setup vertex attributes
        _gl.EnableVertexAttribArray(_attribLocationVtxPos);
        _gl.EnableVertexAttribArray(_attribLocationVtxUV);
        _gl.EnableVertexAttribArray(_attribLocationVtxColor);
        _gl.VertexAttribPointer(_attribLocationVtxPos, 2, VertexAttribPointerType.Float, false, (uint)sizeof(ImDrawVert), (void*)0);
        _gl.VertexAttribPointer(_attribLocationVtxUV, 2, VertexAttribPointerType.Float, false, (uint)sizeof(ImDrawVert), (void*)8);
        _gl.VertexAttribPointer(_attribLocationVtxColor, 4, VertexAttribPointerType.UnsignedByte, true, (uint)sizeof(ImDrawVert), (void*)16);

        var clipOff = drawData.DisplayPos;
        var clipScale = drawData.FramebufferScale;

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = drawData.CmdLists[n];

            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(cmdList.VtxBuffer.Size * sizeof(ImDrawVert)), (void*)cmdList.VtxBuffer.Data, BufferUsageARB.StreamDraw);
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(cmdList.IdxBuffer.Size * sizeof(ushort)), (void*)cmdList.IdxBuffer.Data, BufferUsageARB.StreamDraw);

            for (int cmdIdx = 0; cmdIdx < cmdList.CmdBuffer.Size; cmdIdx++)
            {
                var pcmd = cmdList.CmdBuffer[cmdIdx];

                if (pcmd.UserCallback != nint.Zero)
                {
                    throw new NotImplementedException("User callbacks are not implemented.");
                }

                var clipMin = new Vector2((pcmd.ClipRect.X - clipOff.X) * clipScale.X, (pcmd.ClipRect.Y - clipOff.Y) * clipScale.Y);
                var clipMax = new Vector2((pcmd.ClipRect.Z - clipOff.X) * clipScale.X, (pcmd.ClipRect.W - clipOff.Y) * clipScale.Y);

                if (clipMax.X <= clipMin.X || clipMax.Y <= clipMin.Y)
                    continue;

                _gl.Scissor((int)clipMin.X, (int)(drawData.DisplaySize.Y * clipScale.Y - clipMax.Y), (uint)(clipMax.X - clipMin.X), (uint)(clipMax.Y - clipMin.Y));

                _gl.BindTexture(TextureTarget.Texture2D, (uint)pcmd.TextureId);
                _gl.DrawElementsBaseVertex(PrimitiveType.Triangles, pcmd.ElemCount, DrawElementsType.UnsignedShort, (void*)(pcmd.IdxOffset * sizeof(ushort)), (int)pcmd.VtxOffset);
            }
        }

        // Restore modified GL state
        _gl.UseProgram((uint)lastProgram);
        _gl.BindTexture(TextureTarget.Texture2D, (uint)lastTexture);
        _gl.ActiveTexture((TextureUnit)lastActiveTexture);
        _gl.BindVertexArray((uint)lastVertexArrayObject);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, (uint)lastArrayBuffer);
        _gl.BlendEquationSeparate((BlendEquationModeEXT)lastBlendEquationRgb, (BlendEquationModeEXT)lastBlendEquationAlpha);
        _gl.BlendFuncSeparate((BlendingFactor)lastBlendSrcRgb, (BlendingFactor)lastBlendDstRgb, (BlendingFactor)lastBlendSrcAlpha, (BlendingFactor)lastBlendDstAlpha);
        if (lastEnableBlend) _gl.Enable(EnableCap.Blend); else _gl.Disable(EnableCap.Blend);
        if (lastEnableCullFace) _gl.Enable(EnableCap.CullFace); else _gl.Disable(EnableCap.CullFace);
        if (lastEnableDepthTest) _gl.Enable(EnableCap.DepthTest); else _gl.Disable(EnableCap.DepthTest);
        if (lastEnableStencilTest) _gl.Enable(EnableCap.StencilTest); else _gl.Disable(EnableCap.StencilTest);
        if (lastEnableScissorTest) _gl.Enable(EnableCap.ScissorTest); else _gl.Disable(EnableCap.ScissorTest);
        _gl.Viewport(lastViewport[0], lastViewport[1], (uint)lastViewport[2], (uint)lastViewport[3]);
        _gl.Scissor(lastScissorBox[0], lastScissorBox[1], (uint)lastScissorBox[2], (uint)lastScissorBox[3]);
    }

    private static ImGuiKey TranslateKey(Key key) => key switch
    {
        Key.Tab => ImGuiKey.Tab,
        Key.Left => ImGuiKey.LeftArrow,
        Key.Right => ImGuiKey.RightArrow,
        Key.Up => ImGuiKey.UpArrow,
        Key.Down => ImGuiKey.DownArrow,
        Key.PageUp => ImGuiKey.PageUp,
        Key.PageDown => ImGuiKey.PageDown,
        Key.Home => ImGuiKey.Home,
        Key.End => ImGuiKey.End,
        Key.Insert => ImGuiKey.Insert,
        Key.Delete => ImGuiKey.Delete,
        Key.Backspace => ImGuiKey.Backspace,
        Key.Space => ImGuiKey.Space,
        Key.Enter => ImGuiKey.Enter,
        Key.Escape => ImGuiKey.Escape,
        Key.Apostrophe => ImGuiKey.Apostrophe,
        Key.Comma => ImGuiKey.Comma,
        Key.Minus => ImGuiKey.Minus,
        Key.Period => ImGuiKey.Period,
        Key.Slash => ImGuiKey.Slash,
        Key.Semicolon => ImGuiKey.Semicolon,
        Key.Equal => ImGuiKey.Equal,
        Key.LeftBracket => ImGuiKey.LeftBracket,
        Key.BackSlash => ImGuiKey.Backslash,
        Key.RightBracket => ImGuiKey.RightBracket,
        Key.GraveAccent => ImGuiKey.GraveAccent,
        Key.CapsLock => ImGuiKey.CapsLock,
        Key.ScrollLock => ImGuiKey.ScrollLock,
        Key.NumLock => ImGuiKey.NumLock,
        Key.PrintScreen => ImGuiKey.PrintScreen,
        Key.Pause => ImGuiKey.Pause,
        Key.Keypad0 => ImGuiKey.Keypad0,
        Key.Keypad1 => ImGuiKey.Keypad1,
        Key.Keypad2 => ImGuiKey.Keypad2,
        Key.Keypad3 => ImGuiKey.Keypad3,
        Key.Keypad4 => ImGuiKey.Keypad4,
        Key.Keypad5 => ImGuiKey.Keypad5,
        Key.Keypad6 => ImGuiKey.Keypad6,
        Key.Keypad7 => ImGuiKey.Keypad7,
        Key.Keypad8 => ImGuiKey.Keypad8,
        Key.Keypad9 => ImGuiKey.Keypad9,
        Key.KeypadDecimal => ImGuiKey.KeypadDecimal,
        Key.KeypadDivide => ImGuiKey.KeypadDivide,
        Key.KeypadMultiply => ImGuiKey.KeypadMultiply,
        Key.KeypadSubtract => ImGuiKey.KeypadSubtract,
        Key.KeypadAdd => ImGuiKey.KeypadAdd,
        Key.KeypadEnter => ImGuiKey.KeypadEnter,
        Key.KeypadEqual => ImGuiKey.KeypadEqual,
        Key.ShiftLeft => ImGuiKey.LeftShift,
        Key.ControlLeft => ImGuiKey.LeftCtrl,
        Key.AltLeft => ImGuiKey.LeftAlt,
        Key.SuperLeft => ImGuiKey.LeftSuper,
        Key.ShiftRight => ImGuiKey.RightShift,
        Key.ControlRight => ImGuiKey.RightCtrl,
        Key.AltRight => ImGuiKey.RightAlt,
        Key.SuperRight => ImGuiKey.RightSuper,
        Key.Menu => ImGuiKey.Menu,
        Key.Number0 => ImGuiKey._0,
        Key.Number1 => ImGuiKey._1,
        Key.Number2 => ImGuiKey._2,
        Key.Number3 => ImGuiKey._3,
        Key.Number4 => ImGuiKey._4,
        Key.Number5 => ImGuiKey._5,
        Key.Number6 => ImGuiKey._6,
        Key.Number7 => ImGuiKey._7,
        Key.Number8 => ImGuiKey._8,
        Key.Number9 => ImGuiKey._9,
        Key.A => ImGuiKey.A,
        Key.B => ImGuiKey.B,
        Key.C => ImGuiKey.C,
        Key.D => ImGuiKey.D,
        Key.E => ImGuiKey.E,
        Key.F => ImGuiKey.F,
        Key.G => ImGuiKey.G,
        Key.H => ImGuiKey.H,
        Key.I => ImGuiKey.I,
        Key.J => ImGuiKey.J,
        Key.K => ImGuiKey.K,
        Key.L => ImGuiKey.L,
        Key.M => ImGuiKey.M,
        Key.N => ImGuiKey.N,
        Key.O => ImGuiKey.O,
        Key.P => ImGuiKey.P,
        Key.Q => ImGuiKey.Q,
        Key.R => ImGuiKey.R,
        Key.S => ImGuiKey.S,
        Key.T => ImGuiKey.T,
        Key.U => ImGuiKey.U,
        Key.V => ImGuiKey.V,
        Key.W => ImGuiKey.W,
        Key.X => ImGuiKey.X,
        Key.Y => ImGuiKey.Y,
        Key.Z => ImGuiKey.Z,
        Key.F1 => ImGuiKey.F1,
        Key.F2 => ImGuiKey.F2,
        Key.F3 => ImGuiKey.F3,
        Key.F4 => ImGuiKey.F4,
        Key.F5 => ImGuiKey.F5,
        Key.F6 => ImGuiKey.F6,
        Key.F7 => ImGuiKey.F7,
        Key.F8 => ImGuiKey.F8,
        Key.F9 => ImGuiKey.F9,
        Key.F10 => ImGuiKey.F10,
        Key.F11 => ImGuiKey.F11,
        Key.F12 => ImGuiKey.F12,
        _ => ImGuiKey.None
    };

    public void Dispose()
    {
        foreach (var keyboard in _input.Keyboards)
        {
            keyboard.KeyDown -= OnKeyDown;
            keyboard.KeyUp -= OnKeyUp;
            keyboard.KeyChar -= OnKeyChar;
        }

        foreach (var mouse in _input.Mice)
        {
            mouse.MouseMove -= OnMouseMove;
            mouse.MouseDown -= OnMouseDown;
            mouse.MouseUp -= OnMouseUp;
            mouse.Scroll -= OnScroll;
        }

        // Only dispose if we created the input context ourselves
        if (_ownsInputContext)
        {
            _input.Dispose();
        }

        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteTexture(_fontTexture);
        _gl.DeleteProgram(_shaderProgram);
    }
}
