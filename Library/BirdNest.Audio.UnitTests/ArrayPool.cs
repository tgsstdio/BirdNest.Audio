using NUnit.Framework;
using System;
using System.Collections.Concurrent;

namespace BirdNest.Audio.UnitTests
{
	public class ArrayPool<TClass>
	{
		public class ObjectPool<T>
		{
			private ConcurrentBag<T> _objects;
			private Func<T> _objectGenerator;

			public ObjectPool(Func<T> objectGenerator)
			{
				if (objectGenerator == null) throw new ArgumentNullException("objectGenerator");
				_objects = new ConcurrentBag<T>();
				_objectGenerator = objectGenerator;
			}

			public T GetObject()
			{
				T item;
				if (_objects.TryTake(out item)) return item;
				return _objectGenerator();
			}

			public void PutObject(T item)
			{
				_objects.Add(item);
			}
		}

		private object mLock = new object();
		private ArrayPoolNode<TClass> mRoot;
		private ObjectPool<ArrayPoolNode<TClass>> mNodePool;

		public ArrayPool ()
		{
			mNodePool = new ObjectPool<ArrayPoolNode<TClass>> (() => new ArrayPoolNode<TClass>() );
		}

		public bool Take (int i, out TClass[] buffer)
		{
			throw new NotImplementedException ();
//			lock (mLock)
//			{
//				if (mRoot == null)
//				{
//					buffer = new TClass[i];
//					return true;
//				}
//				else
//				{
//					if (mRoot.Data.Length >= i)
//					{
//						buffer = mRoot.Data;
//						mRoot.Data = null;
//						mNodePool.PutObject (mRoot);
//						mRoot = null;
//					}
//					else
//					{
//
//					}
//				}
//			}
		}

		public void Release (TClass[] buffer)
		{
			throw new NotImplementedException ();
		}
	}

}

