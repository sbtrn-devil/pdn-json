using PaintDotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Drawing;
using System.Drawing.Imaging;

// This version is for older (<=4.0.x) Paint.NET versions

namespace PdnJsonFileTypeOld
{
	public sealed class PdnJsonFileTypeOldPluginFactory : IFileTypeFactory
	{
		public FileType[] GetFileTypeInstances()
		{
			return new[] { new PdnJsonFileTypeOldPlugin() };
		}
	}

	[PluginSupportInfo(typeof(PluginSupportInfo))]
	public class PdnJsonFileTypeOldPlugin : FileType
	{
		private const string HeaderSignature = ".PDN";

		public PdnJsonFileTypeOldPlugin() : base ("PDN via JSON", FileTypeFlags.SupportsSaving |
			FileTypeFlags.SupportsLoading |
			FileTypeFlags.SupportsLayers,
			new string[] { ".pdn-json" })
	    {
	    }

		/// <summary>
		/// Determines if the document was saved without altering the pixel values.
		///
		/// Any settings that change the pixel values should return 'false'.
		///
		/// Because Paint.NET prompts the user to flatten the image, flattening should not be
		/// considered.
		/// For example, a 32-bit PNG will return 'true' even if the document has multiple layers.
		/// </summary>
		public override bool IsReflexive(SaveConfigToken token)
		{
			return true;
		}

		/// <summary>
		/// Saves a document to a stream respecting the properties
		/// </summary>
		protected override void OnSave(Document input, Stream output, SaveConfigToken token, Surface scratchSurface, ProgressEventHandler progressCallback)
		{
			PJSFile pjsFile = new PJSFile();
			pjsFile.width = input.Width;
			pjsFile.height = input.Height;

			foreach (Layer layer in input.Layers)
			{
				BitmapLayer pdnLayer = layer as BitmapLayer;
				PJSLayer pjsLayer = new PJSLayer();

				// transfer layer properties to its PJS representation
				pjsLayer.name = pdnLayer.Name;
				pjsLayer.width = pdnLayer.Width;
				pjsLayer.height = pdnLayer.Height;
				pjsLayer.blendMode = pdnLayer.BlendMode.ToString();
				pjsLayer.visible = pdnLayer.Visible;
				pjsLayer.opacity = pdnLayer.Opacity;

				// transfer the data (as mimeType + base64 encoded image file - we'll use PNG)
				pjsLayer.mimeType = "image/png";
				pjsLayer.base64 = "";

				using (MemoryStream bmpStream = new MemoryStream())
				{
					using (Bitmap bmp = pdnLayer.Surface.CreateAliasedBitmap())
					{
						bmp.Save(bmpStream, ImageFormat.Png);
					}

					pjsLayer.base64 = Convert.ToBase64String(bmpStream.ToArray());
				}

				pjsFile.layers.Add(pjsLayer);
			}

			// write the PJS representation
			DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(PJSFile));
			ser.WriteObject(output, pjsFile);
		}

		/// <summary>
		/// Creates a document from a stream
		/// </summary>
		protected override Document OnLoad(Stream input)
		{
			Document doc = null;
			PJSFile pjsFile = null;

			try {
				DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(PJSFile));
				pjsFile = ser.ReadObject(input) as PJSFile;

				if (pjsFile.width <= 0 || pjsFile.height <= 0)
				{
					throw new ArgumentOutOfRangeException("Invalid document dimensions");
				}

				doc = new Document(pjsFile.width, pjsFile.height);

				foreach (PJSLayer pjsLayer in pjsFile.layers)
				{
					// the layer width/height are apparently not used by the PDN per se, but we'll store
					// and check them for sanity anyway
					if (pjsLayer.width <= 0 || pjsLayer.height <= 0)
					{
						throw new ArgumentOutOfRangeException("Invalid layer dimensions");
					}

					// note: pjsLayer.mimeType can hint on the image type, but is actually redundant in .net -
					// the loader auto-detects the content by the actual data bytes

					byte[] imageData = Convert.FromBase64String(pjsLayer.base64);
					using (Stream imageDataStream = new MemoryStream(imageData))
					{
						using (Image image = Image.FromStream(imageDataStream))
						{
							Surface surface = Surface.CopyFromGdipImage(image);
							// construct BitmapLayer from Surface that will also take its ownership
							BitmapLayer pdnLayer = new BitmapLayer(surface, true);

							// copy the properties
							pdnLayer.Visible = pjsLayer.visible;
							pdnLayer.Opacity = pjsLayer.opacity;
							pdnLayer.Name = pjsLayer.name;

							LayerBlendMode blendMode;
							if (!Enum.TryParse<LayerBlendMode>(pjsLayer.blendMode, out blendMode))
							{
								blendMode = LayerBlendMode.Normal;
							}
							pdnLayer.BlendMode = blendMode;

							// the layer is ready
							doc.Layers.Add(pdnLayer);
						}
					}

				}
			}
			catch (Exception e) {
				if (doc != null)
				{
					doc.Dispose();
				}
				throw new FormatException("Error loading file - " + e.Message, e);
			}

			return doc;
		}
	}

	[DataContract]
	internal class PJSLayer
	{
		[DataMember] internal int width;
		[DataMember] internal int height;
		[DataMember] internal bool visible;
		[DataMember] internal byte opacity;
		[DataMember] internal String name;
		[DataMember] internal String blendMode;
		[DataMember] internal String mimeType;
		[DataMember] internal String base64;
	}

	[DataContract]
	internal class PJSFile
	{
		[DataMember] internal HashSet<String> features = new HashSet<String>();
		[DataMember] internal int width;
		[DataMember] internal int height;

		[DataMember]
		internal List<PJSLayer> layers = new List<PJSLayer>();
	}

	internal static class Features
	{
		// any strings that can go to "features" array are to be defined and referenced via this class
		internal const String RESERVED = "RESERVED";
	}
}
