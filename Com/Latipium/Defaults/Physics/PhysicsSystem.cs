// PhysicsSystem.cs
//
// Copyright (c) 2016 Zach Deibert.
// All Rights Reserved.
using System;
using System.Collections.Generic;
using Jitter;
using Jitter.Collision;
using Jitter.Collision.Shapes;
using Jitter.Dynamics;
using Jitter.LinearMath;
using Com.Latipium.Core;

namespace Com.Latipium.Defaults.Physics {
	internal class PhysicsSystem {
		private readonly Dictionary<LatipiumObject, RigidBody> Bodies;
		private readonly HashSet<LatipiumObject> InitializedTypes;
		private readonly CollisionSystem Collision;
		private readonly World World;

		private void UpdatedCallback(IEnumerable<LatipiumObject> objects) {
			RemoveObjects(objects);
			AddObjects(objects);
		}

		internal void AddObjects(IEnumerable<LatipiumObject> objects) {
			foreach ( LatipiumObject obj in objects ) {
				LatipiumObject type = obj.InvokeFunction<LatipiumObject>("Type");
				if ( type != null ) {
					if ( !InitializedTypes.Contains(type) ) {
						type.InvokeProcedure<Action<IEnumerable<LatipiumObject>>>("Initialize", UpdatedCallback);
					}
					Tuple<float[], int[]> data = type.InvokeFunction<Tuple<float[], int[]>>("GetPhysicsData");
					if ( data != null && data.Object1 != null && data.Object2 != null ) {
						List<JVector> points = new List<JVector>();
						List<TriangleVertexIndices> tris = new List<TriangleVertexIndices>();
						for ( int i = 0; i < data.Object1.Length - 2; i += 3 ) {
							points.Add(new JVector(data.Object1[i], data.Object1[i + 1], data.Object1[i + 2]));
						}
						for ( int i = 0; i < data.Object2.Length - 2; i += 3 ) {
							tris.Add(new TriangleVertexIndices(data.Object2[i], data.Object2[i + 1], data.Object2[i + 2]));
						}
						Octree octree = new Octree(points, tris);
						Shape shape = new TriangleMeshShape(octree);
						RigidBody body = new RigidBody(shape);
						Func<bool> UseGravity = type.GetFunction<bool>("UseGravity");
						body.AffectedByGravity = UseGravity == null || UseGravity();
						Bodies.Add(obj, body);
						World.AddBody(body);
					}
				}
			}
		}

		internal void RemoveObjects(IEnumerable<LatipiumObject> objects) {
			foreach ( LatipiumObject obj in objects ) {
				if ( Bodies.ContainsKey(obj) ) {
					World.RemoveBody(Bodies[obj]);
					Bodies.Remove(obj);
				}
			}
		}

		internal void Step(float time) {
			World.Step(time, true);
		}

		internal PhysicsSystem() {
			Bodies = new Dictionary<LatipiumObject, RigidBody>();
			InitializedTypes = new HashSet<LatipiumObject>();
			Collision = new CollisionSystemSAP();
			World = new World(Collision);
		}
	}
}

