# SlimDX.D3D11Control
SlimDX.D3D11Control is a D3D11-aware WinForms control for use with SlimDX. It takes care of the boilerplate of device context initialization and swapchain setup. It also provides a couple of useful properties for configuring vertical synchronization, setting the rasterizer's cull- and fillmode, etc. but otherwise won't get in your way.

### Installation

SlimDX.D3D11Control is available as a [NuGet Package](https://www.nuget.org/packages/SlimDX.D3D11Control/) downloadable from within Visual Studio. Simply add the package to your project by right-clicking on references and selecting Manage NuGet packages. Once the dialog appears, enter SlimDX.D3D11Control into the search box in the top right corner. After a few seconds, the results will be displayed in the centre panel. Select the SlimDX.D3D11Control package and click Install.

### Usage

Once installed, the D3D11Control component should automatically popup in the Toolbox window from which you can then simply drag it onto a form.

### Documentation

You can browse the online documentation [here](http://smiley22.github.io/SlimDX.D3D11Control/Documentation/). A minimalistic example application can
be found in the /Example directory of the solution. You can also take a look at the [MultiRes3d](https://github.com/smiley22/MultiRes3d) application
which makes use of the control.

<p align="center">
 <img src="http://smiley22.github.io/SlimDX.D3D11Control/Images/SlimDX.D3D11Control.1.png" alt="Simple WinForms Application" />
</p>


### Credits

This library is copyright © 2014-2015 Torben Könke.

### License

This library is released under the [MIT license](https://github.com/qwert9001/SlimDX.D3D11Control/blob/master/LICENSE).

