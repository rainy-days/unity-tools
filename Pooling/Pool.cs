using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RainyDays
{
	/// <summary>
	/// A simple pool of prefab instances.
	/// </summary>
	/// <description>
	/// Useful to reduce the cost of creating objects on the fly from template prefabs. You essentially save the time
	/// it usually takes to allocate memory for the new instance, and deep copy the template onto the new instance.
	/// 
	/// The pool has a fixed maximum capacity defined at design time. It cannot grow further. Ask and Take requests
	/// will fail if the pool is at capacity.
	/// 
	/// API:
	/// * Ask: Politely ask for an instance, and receive it at some point. Frame slicing is based on maxRequestsPerFrame.
	/// * Take: If an instance is available, take it right now, creating it if necessary.
	/// * TakeWarm: If a previously created instance is available, take it right now; otherwise return null.
	/// * Return: Return that instance to the pool for future use.
	/// * Steal: Steal that instance from the pool. Do whatever you want with this instance afterwards.
	/// 
	/// ISSUES:
	/// * Some Unity components (e.g. Animator) have costly activation logic attached, which makes
	///   calls to GameObject.SetActive prohibitive. This Pool class disables individual components instead
	///   of deactivating the game object to avoid this problem. To change this behavior, one can derive from
	///   this class and override the InstanceCreated/Taken/Returned protected methods.
	/// </description>
	public class Pool : MonoBehaviour
	{
		[SerializeField]
		[Tooltip("The template used to create instances")]
		private GameObject _prefab;

		[SerializeField]
		[Tooltip("Fixed, maximum number of instances this pool can hold")]
		private int _capacity;

		[SerializeField]
		[Tooltip("Number of instances to create on Awake")]
		private int _prewarm;

		[SerializeField]
		[Tooltip("Maximum number of Ask requests to process per frame")]
		private int _maxRequestsPerFrame = 1;

		public int Capacity { get { return _capacity; } }
		public int Allocated { get; private set; }
		public int Free { get { return _free.Count; } }
		public int Used { get { return _capacity - _free.Count; } }
		public int PendingRequests { get { return _requests.Count; } }

		private PooledObject[] _pool;
		private Stack<int> _free;

		public class Request
		{
			public Transform parent;
			public PooledObject result;
			public bool isDone;
			public System.Action<Request> callback;
		}
		private Queue<Request> _requests;

		#region Unity API

		private void OnValidate()
		{
			_capacity = Mathf.Max (0, _capacity);
			_prewarm = Mathf.Clamp (_prewarm, 0, _capacity);
			_maxRequestsPerFrame = Mathf.Max (1, _maxRequestsPerFrame);
		}

		private void Awake()
		{
			_requests = new Queue<Request> (_capacity);
			_pool = new PooledObject[_capacity];
			_free = new Stack<int> (_capacity);
			for (int i = _capacity - 1; i >= 0; --i) {
				_free.Push (i);
			}
			for (int i = 0; i < _prewarm; ++i) {
				Take (null);
			}
			for (int i = 0; i < _prewarm; ++i) {
				Return (_pool[i]);
			}
		}

		private void Update()
		{
			if (_requests.Count > 0) {
				int remaining = _maxRequestsPerFrame;
				while (_requests.Count > 0 && remaining > 0) {
					var r = _requests.Dequeue ();
					--remaining;

					r.result = Take (r.parent);
					r.isDone = true;
					if (r.callback != null) {
						r.callback (r);
					}
				}
			}
		}

		private void OnDestroy()
		{
			StealAll ();
		}

		#endregion

		public bool Steal(PooledObject pObj)
		{
			if (!pObj || pObj.Pool != this) {
				return false;
			}

			_pool [pObj.IndexInPool] = null;
			_free.Push (pObj.IndexInPool);
			pObj.Pool = null;
			--Allocated;

			// destroy the component
			Object.Destroy (pObj);
			return true;
		}

		public Request Ask(Transform parent, System.Action<Request> callback)
		{
			if (_requests.Count == _pool.Length) {
				return null;
			}
			var r = new Request () { parent = parent, callback = callback };
			_requests.Enqueue (r);
			return r;
		}

		public PooledObject TakeWarm(Transform parent)
		{
			return Take (parent, true);
		}

		public PooledObject Take(Transform parent)
		{
			return Take (parent, false);
		}

		public PooledObject Take(Transform parent, bool onlyIfWarm)
		{
			if (_free.Count == 0) {
				Debug.LogError ("Pool reached its capacity", this);
				return null;
			}
			int index = _free.Peek ();
			PooledObject obj = _pool [index];
			if (!obj) {
				if (onlyIfWarm) {
					return null;
				}
				Profiler.BeginSample ("Pool.Instantiate", this);
				UnityEngine.Assertions.Assert.IsTrue (_prefab);
				var go = (GameObject)Object.Instantiate (_prefab, Vector3.zero, Quaternion.identity);
				_pool[index] = obj = go.AddComponent<PooledObject> ();
				++Allocated;
				obj.Pool = this;
				obj.IndexInPool = index;

				InstanceCreated (obj);

				Profiler.EndSample ();
			}
			_free.Pop ();
			obj.IsActiveInPool = true;
			if (parent) {
				obj.transform.SetParent (parent, false);
			} else {
				obj.transform.SetParent (this.transform, false);
			}
			InstanceTaken (obj);
			return obj;
		}

		static Dictionary<System.Type, System.Reflection.PropertyInfo> sTypeEnableMap;

		static Pool()
		{
			sTypeEnableMap = new Dictionary<System.Type, System.Reflection.PropertyInfo> ();

			// the "enabled" property is not part of a single base Component class
			// as of 5.3, it is duplicated in: Behaviour, Cloth, Collider, LODGroup, ParticleEmitter, Renderer
			// note: in the Editor, a MonoBehaviour will not have an "enabled" checkbox if it doesn't contain
			//       any Unity messages like Start, OnEnable, OnDisable, Update, ..., but excluding Awake and OnDestroy.

			foreach (var t in typeof(Component).Assembly.GetTypes()) {
				if (t.BaseType == typeof(Component)) {
					var enabledProp = t.GetProperty ("enabled", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
					if (enabledProp != null) {
						sTypeEnableMap [t] = enabledProp;
					}
				}
			}
		}

		protected virtual void InstanceCreated(PooledObject pObj)
		{
			pObj.ManagedComponents = new Dictionary<System.Type, Component[]> ();
			foreach (var kvp in sTypeEnableMap) {
				pObj.ManagedComponents[kvp.Key] = pObj.GetComponentsInChildren (kvp.Key, false);
			}
		}

		protected virtual void InstanceTaken(PooledObject pObj)
		{
			foreach (var kvp in pObj.ManagedComponents) {
				foreach (var c in kvp.Value) {
					sTypeEnableMap [kvp.Key].SetValue (c, true, null);
				}
			}
		}

		public bool Return(PooledObject pObj)
		{
			if (!pObj || pObj.Pool != this) {
				Debug.LogError ("Cannot release object from pool", this);
				return false;
			}

			if (!pObj.IsActiveInPool) {
				return false;
			}

			int index = pObj.IndexInPool;
			UnityEngine.Assertions.Assert.IsTrue (object.ReferenceEquals(pObj, _pool[pObj.IndexInPool]));

			pObj.IsActiveInPool = false;
			_free.Push (index);

			InstanceReturned (pObj);
			return true;
		}

		protected virtual void InstanceReturned(PooledObject pObj)
		{

			foreach (var kvp in pObj.ManagedComponents) {
				foreach (var c in kvp.Value) {
					sTypeEnableMap [kvp.Key].SetValue (c, false, null);
				}
			}

			// once disabled, parent under pool object
			pObj.transform.SetParent (this.transform, false);
		}

		public void ReturnAll()
		{
			for (int i = 0; i < _pool.Length; ++i) {
				var obj = _pool [i];
				if (obj) {
					Return (obj);
				}
			}
		}

		public void StealAll()
		{
			for (int i = 0; i < _pool.Length; ++i) {
				var obj = _pool [i];
				if (obj) {
					Steal (obj);
				}
			}
		}
	}
}
