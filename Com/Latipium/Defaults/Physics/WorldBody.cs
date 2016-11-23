// WorldBody.cs
//
// Copyright (c) 2016 Zach Deibert.
// All Rights Reserved.
using System;
using System.Collections.Generic;
using Jitter.Collision;
using Jitter.Collision.Shapes;
using Jitter.Dynamics;
using Jitter.LinearMath;
using Com.Latipium.Core;

namespace Com.Latipium.Defaults.Physics {
	internal class WorldBody : RigidBody {
		private readonly LatipiumObject Instance;
        private readonly Func<Com.Latipium.Core.Tuple<float, float, float>, Com.Latipium.Core.Tuple<float, float, float>> PositionFunc;
		private readonly Func<float[], float[]> TransformFunc;

		public override void PreStep(float timestep) {
            Com.Latipium.Core.Tuple<float, float, float> position = PositionFunc(null);
			Position = new JVector(position.Object1, position.Object2, position.Object3);
			float[] rotation = TransformFunc(null);
			Orientation = new JMatrix(
				rotation[ 0], rotation[ 1], rotation[ 2], // rotation[ 3],
				rotation[ 4], rotation[ 5], rotation[ 6], // rotation[ 7],
				rotation[ 8], rotation[ 9], rotation[10]  // rotation[11],
			 // rotation[12], rotation[13], rotation[14],    rotation[15]
			);
		}

		public override void PostStep(float timestep) {
			JVector position = Position;
            PositionFunc(new Com.Latipium.Core.Tuple<float, float, float>(position.X, position.Y, position.Z));
			JMatrix orientation = Orientation;
			float[] matrix = TransformFunc(null);
			matrix[ 0] = orientation.M11; matrix[ 1] = orientation.M12; matrix[ 2] = orientation.M13; // matrix[ 3] = orientation.M14;
			matrix[ 4] = orientation.M21; matrix[ 5] = orientation.M22; matrix[ 6] = orientation.M23; // matrix[ 7] = orientation.M24;
			matrix[ 8] = orientation.M31; matrix[ 9] = orientation.M32; matrix[10] = orientation.M33; // matrix[11] = orientation.M34;
		 // matrix[12] = orientation.M41; matrix[13] = orientation.M42; matrix[14] = orientation.M43;    matrix[15] = orientation.M44;
		}

        private static Octree CreateOctree(Com.Latipium.Core.Tuple<float[], int[]> data) {
			List<JVector> points = new List<JVector>();
			List<TriangleVertexIndices> tris = new List<TriangleVertexIndices>();
			for ( int i = 0; i < data.Object1.Length - 2; i += 3 ) {
				points.Add(new JVector(data.Object1[i], data.Object1[i + 1], data.Object1[i + 2]));
			}
			for ( int i = 0; i < data.Object2.Length - 2; i += 3 ) {
				tris.Add(new TriangleVertexIndices(data.Object2[i], data.Object2[i + 1], data.Object2[i + 2]));
			}
			return new Octree(points, tris);
		}

        internal WorldBody(Com.Latipium.Core.Tuple<float[], int[]> data, LatipiumObject obj) : base(new TriangleMeshShape(CreateOctree(data))) {
			obj = Instance;
            PositionFunc = Instance.GetFunction<Com.Latipium.Core.Tuple<float, float, float>, Com.Latipium.Core.Tuple<float, float, float>>("Position");
			TransformFunc = Instance.GetFunction<float[], float[]>("Transform");
		}
	}
}

