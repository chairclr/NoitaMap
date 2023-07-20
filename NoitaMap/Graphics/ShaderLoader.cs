using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace NoitaMap.Graphics;

public static class ShaderLoader
{
    public static (Shader[], VertexElementDescription[], ResourceLayoutDescription[]) Load(GraphicsDevice graphicsDevice, string pixelShader, string vertexShader)
    {
        string shaderPath = Path.Combine(File.Exists(typeof(ShaderLoader).Assembly.Location) ? Path.GetDirectoryName(typeof(ShaderLoader).Assembly.Location)! : Environment.CurrentDirectory, "Assets", "Shaders", "Compiled");

        byte[] vertexShaderBytes = File.ReadAllBytes(Path.Combine(shaderPath, $"{vertexShader}.spirv"));
        byte[] pixelShaderBytes = File.ReadAllBytes(Path.Combine(shaderPath, $"{pixelShader}.spirv"));

        VertexElementDescription[] vertexElementDescriptions;
        ResourceLayoutDescription[] resourceLayoutDescriptions;

        if (graphicsDevice.BackendType != GraphicsBackend.Vulkan)
        {
            CrossCompileTarget compileTarget = graphicsDevice.BackendType switch
            {
                GraphicsBackend.Direct3D11 => CrossCompileTarget.HLSL,
                GraphicsBackend.Metal => CrossCompileTarget.MSL,
                GraphicsBackend.OpenGL => CrossCompileTarget.GLSL,
                GraphicsBackend.OpenGLES => CrossCompileTarget.ESSL,
                _ => throw new Exception()
            };

            VertexFragmentCompilationResult result = SpirvCompilation.CompileVertexFragment(vertexShaderBytes, pixelShaderBytes, compileTarget);

            pixelShaderBytes = Encoding.UTF8.GetBytes(result.FragmentShader);
            vertexShaderBytes = Encoding.UTF8.GetBytes(result.VertexShader);

            vertexElementDescriptions = result.Reflection.VertexElements;
            resourceLayoutDescriptions = result.Reflection.ResourceLayouts;
        }
        else
        {
            // TODO: Deal with it later
            throw new NotImplementedException();
        }

        Shader vs = graphicsDevice.ResourceFactory.CreateShader(new ShaderDescription()
        {
#if DEBUG
            Debug = true,
#endif
            ShaderBytes = vertexShaderBytes,
            Stage = ShaderStages.Vertex,
            EntryPoint = "main"
        });

        Shader ps = graphicsDevice.ResourceFactory.CreateShader(new ShaderDescription()
        {
#if DEBUG
            Debug = true,
#endif
            ShaderBytes = pixelShaderBytes,
            Stage = ShaderStages.Fragment,
            EntryPoint = "main"
        });

        return (new Shader[] { vs, ps }, vertexElementDescriptions, resourceLayoutDescriptions);
    }
}
