using StbImageSharp;
using OpenTK.Graphics.OpenGL4;
using RCS;

namespace RERL.Loaders
{
	public enum TextureType
	{
		Albedo,
		Normal
	}
	
	public static class ImageLoader
	{
		public static int LoadTexture(string imagePath, TextureType type)
		{
			try {
				using var stream = File.OpenRead(imagePath);
				//StbImage.stbi_set_flip_vertically_on_load(type == TextureType.Albedo ? 1 : 0);
				StbImage.stbi_set_flip_vertically_on_load(1);
				var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

				if (image == null) {
					Logger.Warning($"Failed to decode image: {imagePath}");
					return -1;
				}
				
				int internalFormat;
				switch (type) {
					case TextureType.Normal:
						internalFormat = (int)PixelInternalFormat.Rgba;
						break;
					case TextureType.Albedo:
					default:
						internalFormat = (int)PixelInternalFormat.SrgbAlpha;
						break;
				}

				int textureHandle = GL.GenTexture();
				GL.BindTexture(TextureTarget.Texture2D, textureHandle);

				GL.TexImage2D(TextureTarget.Texture2D, 0, (PixelInternalFormat)internalFormat,
					image.Width, image.Height, 0,
					PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);

				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
				GL.GetFloat((GetPName)All.MaxTextureMaxAnisotropy, out float maxAniso);
				GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)All.TextureMaxAnisotropyExt, MathF.Min(16f, maxAniso));
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

				GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

				return textureHandle;
			} catch (Exception ex) {
				Logger.Warning($"Failed to load texture: {ex.Message}");
				return -1;
			}
		}
	}
}