using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

[RegisterService<MinecraftHeadRenderer>(LifeTime.Scoped)]
public class MinecraftHeadRenderer(HttpClient httpClient)
{
	public record RenderOptions(
		string SkinUrl,
		int Size,
		float YawInDegrees,
		float PitchInDegrees,
		float RollInDegrees,
		bool ShowOverlay = true);
	
	public enum IsometricSide { Left, Right }
	public record IsometricRenderOptions(string SkinUrl, int Size, IsometricSide Side = IsometricSide.Right, bool ShowOverlay = true);
	
	private static readonly Dictionary<Face, Rectangle> BaseMappings = new()
	{
		{ Face.Right, new Rectangle(0, 8, 8, 8) },
		{ Face.Front, new Rectangle(8, 8, 8, 8) },
		{ Face.Left, new Rectangle(16, 8, 8, 8) },
		{ Face.Back, new Rectangle(24, 8, 8, 8) },
		{ Face.Top, new Rectangle(8, 0, 8, 8) },
		{ Face.Bottom, new Rectangle(16, 0, 8, 8) }
	};

	private static readonly Dictionary<Face, Rectangle> OverlayMappings = new()
	{
		{ Face.Right, new Rectangle(32, 8, 8, 8) },
		{ Face.Front, new Rectangle(40, 8, 8, 8) },
		{ Face.Left, new Rectangle(48, 8, 8, 8) },
		{ Face.Back, new Rectangle(56, 8, 8, 8) },
		{ Face.Top, new Rectangle(40, 0, 8, 8) },
		{ Face.Bottom, new Rectangle(48, 0, 8, 8) }
	};

	public Task<Image<Rgba32>> RenderIsometricHeadAsync(IsometricRenderOptions options, CancellationToken ct = default)
	{
		// Isometric view: showing front, right, and top faces (or left if specified)
		const float isometricRightYaw = -135f;
		const float isometricLeftYaw = 45f;
		const float isometricPitch = 30f;
		const float isometricRoll = 0f;

		var fullOptions = new RenderOptions(
			options.SkinUrl,
			options.Size,
			options.Side == IsometricSide.Right ? isometricRightYaw : isometricLeftYaw,
			isometricPitch,
			isometricRoll,
			options.ShowOverlay
		);

		return RenderHeadAsync(fullOptions, ct);
	}

	public async Task<Image<Rgba32>> RenderHeadAsync(RenderOptions options, CancellationToken ct = default)
	{
		using var skin = await Image.LoadAsync<Rgba32>(await httpClient.GetStreamAsync(options.SkinUrl, ct), ct);
		return RenderHead(options, skin);
	}

	public static Image<Rgba32> RenderHead(RenderOptions options, Image<Rgba32> skin)
	{
		// Define cube vertices (unit cube centered at origin)
		var vertices = new Vector3[]
		{
			// Back face vertices (z = -0.5)
			new(-0.5f, -0.5f, -0.5f), // 0: bottom-left-back
			new(0.5f, -0.5f, -0.5f), // 1: bottom-right-back
			new(0.5f, 0.5f, -0.5f), // 2: top-right-back
			new(-0.5f, 0.5f, -0.5f), // 3: top-left-back

			// Front face vertices (z = 0.5)
			new(-0.5f, -0.5f, 0.5f), // 4: bottom-left-front
			new(0.5f, -0.5f, 0.5f), // 5: bottom-right-front
			new(0.5f, 0.5f, 0.5f), // 6: top-right-front
			new(-0.5f, 0.5f, 0.5f) // 7: top-left-front
		};

		var standardUvMap = new Vector2[]
		{
			new(1, 0), new(0, 0), new(0, 1), new(1, 1)
		};
		// I don't know why this face needs to be flipped, but it does
		var backFaceUvMap = new Vector2[] { new(1, 1), new(0, 1), new(0, 0), new(1, 0) };

		// Define faces with correct winding order and UV mappings
		var faceDefinitions = new[]
		{
			// Front face (+Z)
			new FaceData(Face.Front, [vertices[7], vertices[6], vertices[5], vertices[4]], standardUvMap),
			// Back face (-Z)
			new FaceData(Face.Back, [vertices[0], vertices[1], vertices[2], vertices[3]], backFaceUvMap),
			// Right face (+X)
			new FaceData(Face.Right, [vertices[6], vertices[2], vertices[1], vertices[5]], standardUvMap),
			// Left face (-X)
			new FaceData(Face.Left, [vertices[3], vertices[7], vertices[4], vertices[0]], standardUvMap),
			// Top face (+Y)
			new FaceData(Face.Top, [vertices[3], vertices[2], vertices[6], vertices[7]], standardUvMap),
			// Bottom face (-Y)
			new FaceData(Face.Bottom, [vertices[4], vertices[5], vertices[1], vertices[0]], standardUvMap)
		};

		// Create rotation matrices
		const float deg2Rad = MathF.PI / 180f;
		var transform = CreateRotationMatrix(
			options.YawInDegrees * deg2Rad,
			options.PitchInDegrees * deg2Rad,
			options.RollInDegrees * deg2Rad
		);

		// Process base layer
		var visibleTriangles = ProcessFaces(faceDefinitions, transform, skin, false);

		// Process overlay layer if enabled
		if (options.ShowOverlay)
		{
			var overlayTransform = Matrix4x4.CreateScale(1.125f) * transform;
			visibleTriangles.AddRange(ProcessFaces(faceDefinitions, overlayTransform, skin, true));
		}

		// Sort triangles by depth (back to front)
		visibleTriangles.Sort((a, b) => b.Depth.CompareTo(a.Depth));

		// Create output image
		var canvas = new Image<Rgba32>(options.Size, options.Size, Color.Transparent);
		var scale = options.Size / 1.75f;
		var offset = new Vector2(options.Size / 2f);

		// Render triangles
		foreach (var tri in visibleTriangles)
		{
			var p1 = ProjectToScreen(tri.V1, scale, offset);
			var p2 = ProjectToScreen(tri.V2, scale, offset);
			var p3 = ProjectToScreen(tri.V3, scale, offset);

			RasterizeTriangle(canvas, p1, p2, p3, tri.T1, tri.T2, tri.T3, tri.Texture);
		}

		return canvas;
	}

	private static Matrix4x4 CreateRotationMatrix(float yaw, float pitch, float roll)
	{
		// Apply rotations in Y-X-Z order for intuitive control
		var cosY = MathF.Cos(yaw);
		var sinY = MathF.Sin(yaw);
		var cosP = MathF.Cos(pitch);
		var sinP = MathF.Sin(pitch);
		var cosR = MathF.Cos(roll);
		var sinR = MathF.Sin(roll);

		// Combined rotation matrix (Y * X * Z)
		return new Matrix4x4(
			cosY * cosR + sinY * sinP * sinR, -cosY * sinR + sinY * sinP * cosR, sinY * cosP, 0,
			cosP * sinR, cosP * cosR, -sinP, 0,
			-sinY * cosR + cosY * sinP * sinR, sinY * sinR + cosY * sinP * cosR, cosY * cosP, 0,
			0, 0, 0, 1
		);
	}

	private static Vector2 ProjectToScreen(Vector3 point, float scale, Vector2 offset)
	{
		// Orthographic projection to 2D screen space (flip Y for screen coordinates)
		return new Vector2(point.X * scale + offset.X, -point.Y * scale + offset.Y);
		
		// This value (from 0.0 to 1.0) controls the strength of the perspective effect.
		// 0.0 = Fully orthographic (flat, no distortion)
		// 1.0 = Full perspective
		// const float perspectiveAmount = 0.2f;
		//
		// // Calculate the full perspective projection
		// const float cameraDistance = 10.0f; // This can stay fixed
		// const float focalLength = 10.0f;
		// var perspectiveFactor = focalLength / (cameraDistance - point.Z);
		// var perspX = point.X * perspectiveFactor;
		// var perspY = point.Y * perspectiveFactor;

		// // Orthographic projection (no perspective)
		// var orthoX = point.X;
		// var orthoY = point.Y;
		//
		// var finalX = orthoX + (perspX - orthoX) * perspectiveAmount;
		// var finalY = orthoY + (perspY - orthoY) * perspectiveAmount;

		// return new Vector2(
		// 	finalX * scale + offset.X,
		// 	-finalY * scale + offset.Y
		// );
	}

	private static List<VisibleTriangle> ProcessFaces(FaceData[] faces, Matrix4x4 transform, Image<Rgba32> skin,
		bool isOverlay)
	{
		var triangles = new List<VisibleTriangle>();
		var mappings = isOverlay ? OverlayMappings : BaseMappings;

		foreach (var face in faces)
		{
			var texRect = mappings[face.FaceType];

			// Extract texture for this face
			using var faceTexture = skin.Clone(ctx => ctx.Crop(texRect));

			// Transform vertices
			var transformed = new Vector3[4];
			for (var i = 0; i < 4; i++) transformed[i] = Vector3.Transform(face.Vertices[i], transform);

			if (!isOverlay)
			{
				// Calculate face normal for backface culling (optional)
				var normal = Vector3.Cross(
					transformed[1] - transformed[0],
					transformed[2] - transformed[0]
				);

				// Skip back-facing triangles (keep this commented if you want to see all faces)
				if (normal.Z < 0) continue;
			}

			// Calculate average depth for sorting
			var depth = (transformed[0].Z + transformed[1].Z + transformed[2].Z + transformed[3].Z) / 4f;

			// Create two triangles for the quad
			triangles.Add(new VisibleTriangle(
				transformed[0], transformed[1], transformed[2],
				face.UvMap[0], face.UvMap[1], face.UvMap[2],
				faceTexture.Clone(), depth
			));

			triangles.Add(new VisibleTriangle(
				transformed[0], transformed[2], transformed[3],
				face.UvMap[0], face.UvMap[2], face.UvMap[3],
				faceTexture.Clone(), depth
			));
		}

		return triangles;
	}

	private static void RasterizeTriangle(
		Image<Rgba32> canvas,
		Vector2 p1, Vector2 p2, Vector2 p3,
		Vector2 t1, Vector2 t2, Vector2 t3,
		Image<Rgba32> texture)
	{
		var area = (p2.X - p1.X) * (p3.Y - p1.Y) - (p3.X - p1.X) * (p2.Y - p1.Y);

		// If the area is negligible, the triangle is a line or a point. Skip it.
		if (Math.Abs(area) < 0.01f)
		{
			texture.Dispose(); // Still need to dispose the cloned texture
			return;
		}

		// Calculate bounding box
		var minX = (int)Math.Max(0, Math.Min(Math.Min(p1.X, p2.X), p3.X));
		var minY = (int)Math.Max(0, Math.Min(Math.Min(p1.Y, p2.Y), p3.Y));
		var maxX = (int)Math.Min(canvas.Width - 1, Math.Ceiling(Math.Max(Math.Max(p1.X, p2.X), p3.X)));
		var maxY = (int)Math.Min(canvas.Height - 1, Math.Ceiling(Math.Max(Math.Max(p1.Y, p2.Y), p3.Y)));

		// Rasterize triangle
		for (var y = minY; y <= maxY; y++)
		for (var x = minX; x <= maxX; x++)
		{
			var point = new Vector2(x + 0.5f, y + 0.5f);
			var bary = GetBarycentric(p1, p2, p3, point);

			// Check if point is inside triangle
			const float epsilon = 1e-5f;
			if (bary.X < -epsilon || bary.Y < -epsilon || bary.Z < -epsilon) continue;

			// Interpolate texture coordinates
			var texCoord = t1 * bary.X + t2 * bary.Y + t3 * bary.Z;

			// Sample texture
			var texX = (int)Math.Clamp(texCoord.X * texture.Width, 0, texture.Width - 1);
			var texY = (int)Math.Clamp(texCoord.Y * texture.Height, 0, texture.Height - 1);

			var color = texture[texX, texY];
			if (color.A > 10) // Skip nearly transparent pixels
				// Alpha blend if needed, or just overwrite
				canvas[x, y] = color;
		}

		texture.Dispose();
	}

	private static Vector3 GetBarycentric(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
	{
		var v0 = b - a;
		var v1 = c - a;
		var v2 = p - a;

		var d00 = Vector2.Dot(v0, v0);
		var d01 = Vector2.Dot(v0, v1);
		var d11 = Vector2.Dot(v1, v1);
		var d20 = Vector2.Dot(v2, v0);
		var d21 = Vector2.Dot(v2, v1);

		var denom = d00 * d11 - d01 * d01;
		if (Math.Abs(denom) < 1e-6f) return new Vector3(-1, -1, -1);

		var v = (d11 * d20 - d01 * d21) / denom;
		var w = (d00 * d21 - d01 * d20) / denom;
		var u = 1.0f - v - w;

		return new Vector3(u, v, w);
	}

	private enum Face
	{
		Top,
		Bottom,
		Left,
		Right,
		Front,
		Back
	}

	private record FaceData(Face FaceType, Vector3[] Vertices, Vector2[] UvMap);

	private record VisibleTriangle(
		Vector3 V1,
		Vector3 V2,
		Vector3 V3,
		Vector2 T1,
		Vector2 T2,
		Vector2 T3,
		Image<Rgba32> Texture,
		float Depth) : IDisposable
	{
		public void Dispose()
		{
			Texture?.Dispose();
		}
	}
}