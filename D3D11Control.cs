using SlimDX.Direct3D11;
using SlimDX.DXGI;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;
using Device = SlimDX.Direct3D11.Device;
using Resource = SlimDX.Direct3D11.Resource;

namespace SlimDX.Windows {
	/// <summary>
	/// D3D11-aware WinForms control.
	/// The WinForms designer will always call the default constructor.
	/// Inherit from this class and call one of its specialized constructors
	/// to create a Direct3D11 device or swapchain with custom properties.
	/// </summary>
	public partial class D3D11Control : UserControl {
		readonly bool designMode;
		Device device;
		SwapChain swapChain;
		SwapChainDescription? swapChainDescription;
		RenderTargetView renderTargetView;
		Texture2D depthStencilBuffer;
		DepthStencilView depthStencilView;
		RasterizerStateDescription rsDescription = new RasterizerStateDescription() {
			CullMode = CullMode.Back,
			FillMode = FillMode.Solid,
			DepthBias = 0,
			DepthBiasClamp = 0,
			SlopeScaledDepthBias = 0,
			IsFrontCounterclockwise = false,
			IsDepthClipEnabled = true,
			IsAntialiasedLineEnabled = false,
			IsScissorEnabled = false,
			IsMultisampleEnabled = false
		};

		/// <summary>
		/// Occurs when the aspect ratio of the viewport has changed.
		/// </summary>
		[Description("Occurs when the aspect ratio of the viewport has changed.")]
		public event EventHandler AspectRatioChanged;

		/// <summary>
		/// Gets the virtual adapter used to perform rendering and create resources.
		/// </summary>
		/// <exception cref="ObjectDisposedException">
		/// The object has been disposed.
		/// </exception>
		[Browsable(false)]
		public Device Device {
			get {
				CheckDisposed();
				return device;
			}
		}

		/// <summary>
		/// Gets the <c>DeviceContext</c> instance which generates rendering commands.
		/// </summary>
		/// <exception cref="ObjectDisposedException">
		/// The object has been disposed.
		/// </exception>
		[Browsable(false)]
		public DeviceContext Context {
			get {
				CheckDisposed();
				return Device.ImmediateContext;
			}
		}

		/// <summary>
		/// Gets the <c>SwapChain</c> instance associated with the device.
		/// </summary>
		/// <exception cref="ObjectDisposedException">
		/// The object has been disposed.
		/// </exception>
		[Browsable(false)]
		public SwapChain SwapChain {
			get {
				CheckDisposed();
				return swapChain;
			}
		}

		/// <summary>
		/// Gets the <c>RenderTargetView</c> instance associated with the output merger.
		/// </summary>
		/// <exception cref="ObjectDisposedException">
		/// The object has been disposed.
		/// </exception>
		[Browsable(false)]
		public RenderTargetView RenderTargetView {
			get {
				CheckDisposed();
				return renderTargetView;
			}
		}

		/// <summary>
		/// Gets the <c>DepthStencilView</c> instance associated with the output merger.
		/// </summary>
		/// <exception cref="ObjectDisposedException">
		/// The object has been disposed.
		/// </exception>
		[Browsable(false)]
		public DepthStencilView DepthStencilView {
			get {
				CheckDisposed();
				return depthStencilView;
			}
		}

		/// <summary>
		/// Indicates whether the viewport will automatically size itself when the control is resized.
		/// </summary>
		[Description("Indicates whether the viewport will automatically size itself when the control is resized.")]
		[DefaultValue(true)]
		public bool AutoAdjustViewPort {
			get;
			set;
		}

		/// <summary>
		/// Gets the aspect ratio of this <c>D3D11Control</c> instance.
		/// </summary>
		[Description("The aspect ratio of the client area of this D3D11Control instance.")]
		[Browsable(false)]
		public float AspectRatio {
			get {
				CheckDisposed();
				return ClientSize.Width / (float)ClientSize.Height;
			}
		}

		/// <summary>
		/// Gets or sets which triangles are to be culled.
		/// </summary>
		[Description("Indicates which triangles are to be culled.")]
		[DefaultValue(CullMode.Back)]
		public CullMode CullMode {
			get {
				return rsDescription.CullMode;
			}
			set {
				rsDescription.CullMode = value;
				if (!designMode)
					UpdateRasterizerState();
			}
		}

		/// <summary>
		/// Gets or sets the fill mode to use when rendering.
		/// </summary>
		[Description("Determines the fill mode to use when rendering.")]
		[DefaultValue(FillMode.Solid)]
		public FillMode FillMode {
			get {
				return rsDescription.FillMode;
			}
			set {
				rsDescription.FillMode = value;
				if (!designMode)
					UpdateRasterizerState();
			}
		}

		/// <summary>
		/// Determines if a triangle is front- or back-facing.
		/// </summary>
		/// <remarks>
		/// If this parameter is true, then a triangle will be considered front-facing if its
		/// vertices are counter-clockwise on the render target and considered back-facing if they
		/// are clockwise. If this parameter is false then the opposite is true.
		/// </remarks>
		[Description("Determines if a triangle is front- or back-facing.")]
		[DefaultValue(false)]
		public bool FrontCounterClockwise {
			get {
				return rsDescription.IsFrontCounterclockwise;
			}
			set {
				rsDescription.IsFrontCounterclockwise = value;
				if (!designMode)
					UpdateRasterizerState();
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether vertical synchronization (vsync) is active.
		/// </summary>
		[Description("Indicates whether vertical synchronization should be used.")]
		[DefaultValue(false)]
		public bool VSync {
			get;
			set;
		}

		/// <summary>
		/// Gets the <c>CreateParams</c> instance for this <c>D3D11Control</c>.
		/// </summary>
		protected override CreateParams CreateParams {
			get {
				const int CS_VREDRAW = 0x1;
				const int CS_HREDRAW = 0x2;
				const int CS_OWNDC = 0x20;

				var cp = base.CreateParams;
				// Setup necessary class style on windows.
				cp.ClassStyle |= CS_VREDRAW | CS_HREDRAW | CS_OWNDC;
				return cp;
			}
		}

		/// <summary>
		/// Creates a new instance of the D3D11Control class.
		/// </summary>
		/// <exception cref="Direct3D11Exception">
		/// The Direct3D device could not be created. Use the ResultCode property to obtain the
		/// specific error condition.
		/// </exception>
		public D3D11Control()
			: this(DriverType.Hardware, DeviceCreationFlags.None, null, null) {
		}

		/// <summary>
		/// Creates a new instance of the D3D11Controll class with the specified device and swapchain
		/// properties.
		/// </summary>
		/// <param name="type">
		/// The type of device to create.
		/// </param>
		/// <param name="flags">
		/// A list of runtime layers to enable.
		/// </param>
		/// <param name="featureLevels">
		/// A list of feature levels which determine the order of feature levels to attempt to create.
		/// </param>
		/// <param name="swapChainDescription">
		/// The properties to use for creating the swapchain.
		/// </param>
		/// <exception cref="Direct3D11Exception">
		/// The requested device could not be created. Use the ResultCode property to obtain the
		/// specific error condition.
		/// </exception>
		public D3D11Control(DriverType type, DeviceCreationFlags flags, FeatureLevel[] featureLevels,
			SwapChainDescription? swapChainDescription) : base() {
			// Setup control styles for owner draw.
			SetStyle(ControlStyles.Opaque | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint,
				true);
			DoubleBuffered = false;
			AutoAdjustViewPort = true;
			designMode = DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime;
			if (!designMode) {
				// Create the D3D11 device.
				device = new Device(type, flags, featureLevels);
				// Defer swapchain creation until the control's handle has been created.
				this.swapChainDescription = swapChainDescription;
			}
			InitializeComponent();
		}

		/// <summary>
		/// Resizes the viewport to the specified dimensions.
		/// </summary>
		/// <param name="s">
		/// The width and height to resize the viewport to, in pixels.
		/// </param>
		public void ResizeViewport(Size s) {
			var current = Context.Rasterizer.GetViewports();
			var viewport = new Viewport(0, 0, s.Width, s.Height);
			Context.Rasterizer.SetViewports(viewport);
			// See if we need to raise an AspectRatioChanged event.
			if (current.Length > 0) {
				var oldAspectRatio = current[0].Width / current[0].Height;
				if (!NearlyEqual(AspectRatio, oldAspectRatio)) {
					OnAspectRatioChanged(EventArgs.Empty);
				}
			} else {
				OnAspectRatioChanged(EventArgs.Empty);
			}
		}

		/// <summary>
		/// Swaps the front and back buffers, presenting the rendered scene to the screen.
		/// </summary>
		public void Present() {
			swapChain.Present(VSync ? 1 : 0, PresentFlags.None);
		}

		/// <summary>
		/// Clears the back buffer and optionally the depth- and stencil buffers.
		/// </summary>
		/// <param name="color">
		/// The color to fill the back buffer with.
		/// </param>
		/// <param name="clearDepthStencil">
		/// true to clear the depth- and stencil buffers; otherwise false.
		/// </param>
		public void Clear(Color color, bool clearDepthStencil = true) {
			Context.ClearRenderTargetView(renderTargetView, color);
			if (clearDepthStencil) {
				Context.ClearDepthStencilView(depthStencilView,
					DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
			}
		}

		/// <summary>
		/// Raises the System.Windows.Forms.Control.HandleCreated event.
		/// </summary>
		/// <param name="e">
		/// An System.EventArgs that contains the event data.
		/// </param>
		protected override void OnHandleCreated(EventArgs e) {
			base.OnHandleCreated(e);
			if (designMode)
				return;
			// If no swapchain description was provided, use default values.
			var description = swapChainDescription.HasValue ? swapChainDescription.Value :
				new SwapChainDescription() {
					BufferCount = 1,
					Usage = Usage.RenderTargetOutput,
					IsWindowed = true,
					ModeDescription = new ModeDescription(0, 0, new Rational(60, 1), Format.R8G8B8A8_UNorm),
					SampleDescription = new SampleDescription(1, 0),
					Flags = SwapChainFlags.AllowModeSwitch,
					SwapEffect = SwapEffect.Discard
			};
			description.OutputHandle = Handle;
			// The swapchain must be created with the Factory instance that was used to create the
			// device.
			swapChain = new SwapChain(device.Factory, device, description);
			// Simply call OnResize to setup renderTargetView and Depth + Stencil Buffers.
			OnResize(EventArgs.Empty);
		}

		/// <summary>
		/// Raises the System.Windows.Forms.Control.HandleDestroyed event.
		/// </summary>
		/// <param name="e">
		/// An System.EventArgs that contains the event data.
		/// </param>
		protected override void OnHandleDestroyed(EventArgs e) {
			base.OnHandleDestroyed(e);
			ReleaseCOMObjects(true);
		}

		/// <summary>
		/// Raises the System.Windows.Forms.Control.Resize event.
		/// </summary>
		/// <param name="e">
		/// An System.EventArgs that contains the event data.
		/// </param>
		protected override void OnResize(EventArgs e) {
			base.OnResize(e);
			// If the form is minimized, OnResize is triggered with a client-size of (0,0).
			if (ClientSize.IsEmpty)
				return;
			if (swapChain == null)
				return;
			ReleaseCOMObjects(false);
			// Resize the back buffer.
			swapChain.ResizeBuffers(1, 0, 0, Format.R8G8B8A8_UNorm, SwapChainFlags.AllowModeSwitch);
			// Bind the back buffer to the output merger stage of the pipeline so that Direct3D can
			// render onto it. FromSwapChain increases the reference count of the swapChain object, so
			// wrap it in a using statement to keep the reference count at 1.
			using (var resource = Resource.FromSwapChain<Texture2D>(swapChain, 0))
				renderTargetView = new RenderTargetView(device, resource);
			// Create the depth and stencil buffers.
			var depthStencilDesc = new Texture2DDescription {
				Width = ClientSize.Width,
				Height = ClientSize.Height,
				MipLevels = 1,
				ArraySize = 1,
				Format = Format.D24_UNorm_S8_UInt,
				SampleDescription = new SampleDescription(1, 0),
				Usage = ResourceUsage.Default,
				BindFlags = BindFlags.DepthStencil,
				CpuAccessFlags = CpuAccessFlags.None,
				OptionFlags = ResourceOptionFlags.None
			};
			depthStencilBuffer = new Texture2D(device, depthStencilDesc);
			depthStencilView = new DepthStencilView(device, depthStencilBuffer);
			Context.OutputMerger.SetTargets(depthStencilView, renderTargetView);
			// Adjust the viewport
			if (AutoAdjustViewPort)
				ResizeViewport(ClientSize);
		}

		/// <summary>
		/// Raises the System.Windows.Forms.Control.Paint event.
		/// </summary>
		/// <param name="e">
		/// A System.Windows.Forms.PaintEventArgs that contains the event data.
		/// </param>
		protected override void OnPaint(PaintEventArgs e) {
			if (designMode) {
				e.Graphics.Clear(BackColor);
			} else {
				base.OnPaint(e);
				swapChain.Present(0, PresentFlags.None);
			}
		}

		/// <summary>
		/// Raises the AspectRatioChanged event.
		/// </summary>
		/// <param name="e">
		/// An System.EventArgs that contains the event data.
		/// </param>
		protected virtual void OnAspectRatioChanged(EventArgs e) {
			// Best practice.
			var handler = AspectRatioChanged;
			if(handler != null)
				handler(this, e);
		}

		/// <summary>
		/// Throws an ObjectDisposedException if the instance is in the disposed state.
		/// </summary>
		protected void CheckDisposed() {
			if (IsDisposed) {
				throw new ObjectDisposedException(GetType().FullName);
			}
		}

		/// <summary>
		/// Returns a value indicating whether this instance and a specified System.Single object
		/// represent the same value.
		/// </summary>
		/// <param name="a">
		/// The first value to compare.
		/// </param>
		/// <param name="b">
		/// The second value to compare.
		/// </param>
		/// <param name="epsilon">
		/// The epsilon value to use.
		/// </param>
		/// <returns>
		/// true if the values are considered equal; otherwise, false.
		/// </returns>
		[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
		static bool NearlyEqual(float a, float b, float epsilon = .0000001f) {
			var absA = Math.Abs(a);
			var absB = Math.Abs(b);
			var diff = Math.Abs(a - b);
			if (a == b) // shortcut, handles infinities
				return true;
			if (a == 0 || b == 0 || diff < float.Epsilon) {
				// a or b is zero or both are extremely close to it
				// relative error is less meaningful here
				return diff < epsilon;
			}
            // use relative error
			return diff / (absA + absB) < epsilon;
		}

		/// <summary>
		/// Releases various D3D COM resources.
		/// </summary>
		/// <param name="deviceAndSwapChain">
		/// true to release the device and swapchain objects; otherwise false.
		/// </param>
		void ReleaseCOMObjects(bool deviceAndSwapChain) {
			if (renderTargetView != null) {
				renderTargetView.Dispose();
				renderTargetView = null;
			}
			if (depthStencilView != null) {
				depthStencilView.Dispose();
				depthStencilView = null;
			}
			if (depthStencilBuffer != null) {
				depthStencilBuffer.Dispose();
				depthStencilBuffer = null;
			}
			if (deviceAndSwapChain) {
				if (swapChain != null) {
					swapChain.Dispose();
					swapChain = null;
				}
				if (device != null) {
					if (Context.Rasterizer.State != null) {
						Context.Rasterizer.State.Dispose();
					}
					device.Dispose();
					device = null;
				}
			}
		}

		/// <summary>
		/// Updates the rasterizer state for the rasterizer stage of the pipeline.
		/// </summary>
		void UpdateRasterizerState() {
			var rs = RasterizerState.FromDescription(device, rsDescription);
			var oldRs = Context.Rasterizer.State;
			Context.Rasterizer.State = rs;
			if (oldRs != null) {
				oldRs.Dispose();
			}
		}
	}
}
