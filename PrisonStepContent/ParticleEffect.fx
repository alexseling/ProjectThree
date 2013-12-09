float4x4 World;
float4x4 View;
float4x4 Projection;

texture Texture;
float Alpha = 1;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

sampler Sampler = sampler_state
{
    Texture = <Texture>;

    MinFilter = LINEAR;
    MagFilter = LINEAR;
    
    AddressU = Wrap;
    AddressV = Wrap;
    AddressW = Wrap;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;
    output.TexCoord = input.TexCoord;

	// We need the position and normal in world coordinates
    float4 position = mul(input.Position, World);
    float4 viewPosition = mul(position, View);
    output.Position = mul(viewPosition, Projection);
    
    return output;
}

float4 PixelShaderTexturedFunction(VertexShaderOutput input) : COLOR0
{
    return tex2D(Sampler, input.TexCoord) * float4(1, 1, 1, Alpha);
}

technique Textured
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderTexturedFunction();
    }
}
