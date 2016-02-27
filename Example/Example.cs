using System;
using System.Drawing;
using System.Windows.Forms;
using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using Buffer = SlimDX.Direct3D11.Buffer;
using Device = SlimDX.Direct3D11.Device;

namespace Example {
    public partial class Example : Form {
        ShaderSignature inputSignature;
        PixelShader pixelShader;
        VertexShader vertexShader;
        Color backgroundColor = Color.Black;

        Device device {
            get {
                return d3D11Control.Device;
            }
        }

        DeviceContext context {
            get {
                return device.ImmediateContext;
            }
        }

        public Example() {
            InitializeComponent();
        }

        void Example_Load(object sender, EventArgs e) {
            // load and compile the vertex shader
            using (var bytecode = ShaderBytecode.CompileFromFile("triangle.fx", "VShader", "vs_4_0", ShaderFlags.None, EffectFlags.None)) {
                inputSignature = ShaderSignature.GetInputSignature(bytecode);
                vertexShader = new VertexShader(device, bytecode);
            }

            // load and compile the pixel shader
            using (var bytecode = ShaderBytecode.CompileFromFile("triangle.fx", "PShader", "ps_4_0", ShaderFlags.None, EffectFlags.None))
                pixelShader = new PixelShader(device, bytecode);

            // create test vertex data, making sure to rewind the stream afterward
            var vertices = new DataStream(12 * 3, true, true);
            vertices.Write(new Vector3(0.0f, 0.5f, 0.5f));
            vertices.Write(new Vector3(0.5f, -0.5f, 0.5f));
            vertices.Write(new Vector3(-0.5f, -0.5f, 0.5f));
            vertices.Position = 0;

            // create the vertex layout and buffer
            var elements = new[] { new InputElement("POSITION", 0, Format.R32G32B32_Float, 0) };
            var layout = new InputLayout(device, inputSignature, elements);
            var vertexBuffer = new Buffer(device, vertices, 12 * 3, ResourceUsage.Default, BindFlags.VertexBuffer, CpuAccessFlags.None,
                ResourceOptionFlags.None, 0);

            // configure the Input Assembler portion of the pipeline with the vertex data
            context.InputAssembler.InputLayout = layout;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, 12, 0));

            // set the shaders
            context.VertexShader.Set(vertexShader);
            context.PixelShader.Set(pixelShader);

            Render();
        }

        void Render() {
            d3D11Control.Clear(backgroundColor);
            context.Draw(3, 0);
            d3D11Control.Present();
        }

        void red_Click(object sender, EventArgs e) {
            backgroundColor = Color.Red;
            Render();
        }

        void green_Click(object sender, EventArgs e) {
            backgroundColor = Color.Green;                
            Render();
        }

        void blue_Click(object sender, EventArgs e) {
            backgroundColor = Color.Blue;
            Render();
        }

        void flip_Click(object sender, EventArgs e) {
            d3D11Control.FillMode = d3D11Control.FillMode == FillMode.Solid ?
                FillMode.Wireframe : FillMode.Solid;
            flip.Text = d3D11Control.FillMode == FillMode.Solid ? "Wireframe" : "Solid";
            Render();
        }
    }
}
