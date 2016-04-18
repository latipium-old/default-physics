// PhysicsModule.cs
//
// Copyright (c) 2016 Zach Deibert.
// All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Jitter;
using Jitter.Collision;
using Com.Latipium.Core;

namespace Com.Latipium.Defaults.Physics {
	/// <summary>
	/// The default implementation for the physics module.
	/// </summary>
	public class PhysicsModule : AbstractLatipiumModule {
		private LatipiumObject World;
		private Dictionary<LatipiumObject, PhysicsSystem> Systems;
		private Dictionary<LatipiumObject, List<LatipiumObject>> Additions;
		private Dictionary<LatipiumObject, List<LatipiumObject>> Removals;
		private object ListLock;
		private Func<IEnumerable<LatipiumObject>> GetRealms;

		/// <summary>
		/// Occurs when an object is added.
		/// </summary>
		[LatipiumMethod("ObjectAdded")]
		public event Action<LatipiumObject, LatipiumObject> ObjectAdded;
		/// <summary>
		/// Occurs when an object is removed.
		/// </summary>
		[LatipiumMethod("ObjectRemoved")]
		public event Action<LatipiumObject, LatipiumObject> ObjectRemoved;
		/// <summary>
		/// Occurs when a realm is added.
		/// </summary>
		[LatipiumMethod("RealmAdded")]
		public event Action<LatipiumObject> RealmAdded;
		/// <summary>
		/// Occurs when a realm is removed.
		/// </summary>
		[LatipiumMethod("RealmRemoved")]
		public event Action<LatipiumObject> RealmRemoved;

		/// <summary>
		/// Loads the world to use for the physics engine.
		/// </summary>
		/// <param name="world">The world to use.</param>
		[LatipiumMethod("LoadWorld")]
		public void LoadWorld(LatipiumObject world) {
			World = world;
			GetRealms = World.GetFunction<IEnumerable<LatipiumObject>>("GetRealms");
			if ( GetRealms != null ) {
				foreach ( LatipiumObject realm in GetRealms() ) {
					Added(realm);
				}
			}
		}

		/// <summary>
		/// Initializes the module.
		/// </summary>
		[LatipiumMethod("Initialize")]
		public void Init() {
			Systems = new Dictionary<LatipiumObject, PhysicsSystem>();
			Additions = new Dictionary<LatipiumObject, List<LatipiumObject>>();
			Removals = new Dictionary<LatipiumObject, List<LatipiumObject>>();
			ListLock = new object();
		}

		/// <summary>
		/// Runs the main loop.
		/// </summary>
		[LatipiumMethod("Loop")]
		public void Loop() {
			int update = Environment.TickCount;
			while ( true ) {
				float time = Environment.TickCount - update;
				update = Environment.TickCount;
				Dictionary<LatipiumObject, List<LatipiumObject>> objects = new Dictionary<LatipiumObject, List<LatipiumObject>>();
				foreach ( LatipiumObject realm in GetRealms() ) {
					objects.Clear();
					List<LatipiumObject> additions;
					if ( Additions.ContainsKey(realm) ) {
						additions = Additions[realm];
					} else {
						additions = new List<LatipiumObject>();
						Additions[realm] = additions;
					}
					List<LatipiumObject> removals;
					if ( Removals.ContainsKey(realm) ) {
						removals = Removals[realm];
					} else {
						removals = new List<LatipiumObject>();
						Removals[realm] = removals;
					}
					foreach ( LatipiumObject obj in realm.InvokeFunction<IEnumerable<LatipiumObject>>("GetObjects") ) {
						LatipiumObject type = obj.InvokeFunction<LatipiumObject>("Type");
						List<LatipiumObject> list;
						if ( objects.ContainsKey(type) ) {
							list = objects[type];
						} else {
							list = new List<LatipiumObject>();
							objects[type] = list;
						}
						list.Add(obj);
					}
					foreach ( LatipiumObject type in objects.Keys ) {
						Tuple<IEnumerable<LatipiumObject>, IEnumerable<LatipiumObject>> changes = type.InvokeFunction<IEnumerable<LatipiumObject>, Tuple<IEnumerable<LatipiumObject>, IEnumerable<LatipiumObject>>>("PhysicsUpdate", objects[type]);
						if ( changes != null ) {
							lock ( ListLock ) {
								if ( changes.Object1 != null ) {
									additions.AddRange(changes.Object1);
								}
								if ( changes.Object2 != null ) {
									removals.AddRange(changes.Object2);
								}
							}
						}
					}
					Systems[realm].Step(time);
				}
			}
		}

		/// <summary>
		/// Destroys all resources used by the module.
		/// </summary>
		[LatipiumMethod("Destroy")]
		public void Deinit() {
		}

		/// <summary>
		/// Adds/removes pending objects from/to the world.
		/// </summary>
		[LatipiumMethod("Update")]
		public void Update() {
			lock ( ListLock ) {
				foreach ( LatipiumObject realm in Additions.Keys ) {
					List<LatipiumObject> objects = Additions[realm];
					Systems[realm].AddObjects(objects);
					realm.InvokeProcedure<IEnumerable<LatipiumObject>>("AddObject", objects);
					Added(objects, realm);
					objects.Clear();
				}
				foreach ( LatipiumObject realm in Removals.Keys ) {
					List<LatipiumObject> objects = Removals[realm];
					Systems[realm].RemoveObjects(objects);
					realm.InvokeProcedure<IEnumerable<LatipiumObject>>("RemoveObject", objects);
					Removed(objects, realm);
					objects.Clear();
				}
			}
		}

		/// <summary>
		/// Registers an externally added object to the engine.
		/// </summary>
		/// <param name="objs">The list of objects.</param>
		/// <param name="realm">The realm the objects are in.</param>
		[LatipiumMethod("ObjectExternallyAdded")]
		public void Added(IEnumerable<LatipiumObject> objs, LatipiumObject realm) {
			Systems[realm].AddObjects(objs);
			if ( ObjectAdded != null ) {
				foreach ( LatipiumObject obj in objs ) {
					ObjectAdded(obj, realm);
				}
			}
		}

		/// <summary>
		/// Removes an externally removed object from the engine.
		/// </summary>
		/// <param name="objs">The list of objects.</param>
		/// <param name="realm">The realm the objects are in.</param>
		[LatipiumMethod("ObjectExternallyRemoved")]
		public void Removed(IEnumerable<LatipiumObject> objs, LatipiumObject realm) {
			Systems[realm].RemoveObjects(objs);
			if ( ObjectRemoved != null ) {
				foreach ( LatipiumObject obj in objs ) {
					ObjectRemoved(obj, realm);
				}
			}
		}

		/// <summary>
		/// Registers an externally added realm to the engine.
		/// </summary>
		/// <param name="realm">The realm the objects are in.</param>
		[LatipiumMethod("RealmExternallyAdded")]
		public void Added(LatipiumObject realm) {
			PhysicsSystem system = new PhysicsSystem();
			IEnumerable<LatipiumObject> objs = realm.InvokeFunction<IEnumerable<LatipiumObject>>("GetObjects");
			if ( objs != null ) {
				system.AddObjects(objs);
			}
			Systems.Add(realm, system);
			if ( RealmAdded != null ) {
				RealmAdded(realm);
			}
		}

		/// <summary>
		/// Removes an externally removed realm from the engine.
		/// </summary>
		/// <param name="realm">The realm the objects are in.</param>
		[LatipiumMethod("RealmExternallyRemoved")]
		public void Removed(LatipiumObject realm) {
			Systems.Remove(realm);
			if ( RealmRemoved != null ) {
				RealmRemoved(realm);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Com.Latipium.Defaults.Physics.PhysicsModule"/> class.
		/// </summary>
		public PhysicsModule() : base(new string[] { "Com.Latipium.Modules.Physics" }) {
		}
	}
}

