using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace NoitaMap.Graphics;

public static class ShaderLoader
{
    public static Shader[] Load(GraphicsDevice graphicsDevice, string pixelShader, string vertexShader)
    {
        string shaderPath = Path.Combine(PathService.ApplicationPath, "Assets", "Shaders", "Compiled");

        byte[] vertexShaderBytes = File.ReadAllBytes(Path.Combine(shaderPath, $"{vertexShader}.spirv"));
        byte[] pixelShaderBytes = File.ReadAllBytes(Path.Combine(shaderPath, $"{pixelShader}.spirv"));

        string vsEntryPoint = "VSMain";
        string psEntryPoint = "PSMain";

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

            VertexFragmentCompilationResult result = SpirvCompilation.CompileVertexFragment(vertexShaderBytes, pixelShaderBytes, compileTarget, new CrossCompileOptions()
            {
                NormalizeResourceNames = true,
                InvertVertexOutputY = compileTarget == CrossCompileTarget.HLSL,
            });

            vertexShaderBytes = Encoding.UTF8.GetBytes(result.VertexShader);
            pixelShaderBytes = Encoding.UTF8.GetBytes(result.FragmentShader);
        }



        Shader vs = graphicsDevice.ResourceFactory.CreateShader(new ShaderDescription()
        {
#if DEBUG
            Debug = true,
#endif
            ShaderBytes = vertexShaderBytes,
            Stage = ShaderStages.Vertex,
            EntryPoint = vsEntryPoint,
        });

        Shader ps = graphicsDevice.ResourceFactory.CreateShader(new ShaderDescription()
        {
#if DEBUG
            Debug = true,
#endif
            ShaderBytes = pixelShaderBytes,
            Stage = ShaderStages.Fragment,
            EntryPoint = psEntryPoint
        });

        return new Shader[] { vs, ps };
    }
}
