﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace SocketServer
{
	/// <summary>
	/// SocketAsyncEventArgs 管理池
	/// </summary>
	public class SocketAsyncEventArgsPool
	{
		/// <summary>
		/// 数据缓存池
		/// </summary>
		private BufferManager bufferManager;

		/// <summary>
		/// SocketAsyncEventArgs 池
		/// </summary>
		private Stack<SocketAsyncEventArgs> pool;

		/// <summary>
		/// SocketAsyncEventArgs 完成时的方法
		/// </summary>
		private EventHandler<SocketAsyncEventArgs> completed;

		/// <summary>
		/// SocketAsyncEventArgs 缓存最大字节数
		/// </summary>
		private int singleMaxBufferSize;


		/// <summary>
		/// 获取池中 SocketAsyncEventArgs 的数量
		/// </summary>
		public int Count
		{
			get
			{
				return pool.Count;
			}
		}
		


		/// <summary>
		/// 初始化 SocketAsyncEventArgs 池, 并设置 SocketAsyncEventArgs.Buffer 缓存.
		/// </summary>
		/// <param name="capacity">初始状态容量大小</param>
		/// <param name="completed">SocketAsyncEventArgs.Completed 事件执行的方法</param>
		/// <param name="singleMaxBufferSize">SocketAsyncEventArgs.Buffer 的最大 Length, 默认为32K</param>
		public SocketAsyncEventArgsPool( int capacity, EventHandler<SocketAsyncEventArgs> completed, int singleMaxBufferSize = 32 * 1024 )
		{
			this.completed = completed;
			this.singleMaxBufferSize = singleMaxBufferSize;
			//缓存池大小与SocketAsyncEventArgs池大小相同,因为每个SocketAsyncEventArgs只用一个缓存
			bufferManager = BufferManager.CreateBufferManager( capacity, singleMaxBufferSize );
			pool = new Stack<SocketAsyncEventArgs>( capacity );

			for ( int i = 0; i < capacity; i++ )
			{
				SocketAsyncEventArgs arg = this.CreateNew();
				pool.Push( arg );
			}
		}

		/// <summary>
		/// 初始化 SocketAsyncEventArgs 池, 并设置 SocketAsyncEventArgs.Buffer 缓存.
		/// </summary>
		/// <param name="capacity">初始状态容量大小</param>
		/// <param name="singleMaxBufferSize">SocketAsyncEventArgs.Buffer 的最大 Length, 默认为32K</param>
		public SocketAsyncEventArgsPool( int capacity, int singleMaxBufferSize = 32 * 1024 ) : this( capacity, null, singleMaxBufferSize )
		{
		}

		

		/// <summary>
		/// 入栈
		/// </summary>
		/// <param name="item">SocketAsyncEventArgs 实例, 不可为null</param>
		public void Push( SocketAsyncEventArgs item )
		{
			if ( item == null )
			{
				throw new ArgumentNullException( "item" );
			}

			lock ( pool )
			{
				item.AcceptSocket = null;
				item.RemoteEndPoint = null;
				item.UserToken = null;
				item.DisconnectReuseSocket = true;
				pool.Push( item );
			}
		}

		/// <summary>
		/// 出栈, 如果为空则创建新的 SocketAsyncEventArgs 并设置初始值返回
		/// </summary>
		/// <returns>SocketAsyncEventArgs 实例</returns>
		public SocketAsyncEventArgs Pop()
		{
			SocketAsyncEventArgs result = null;

			lock ( pool )
			{
				if ( pool.Count == 0 )
				{
					result = CreateNew();
				}
				else
				{
					result = pool.Pop();
				}
			}

			return result;
		}



		/// <summary>
		/// 创建新 SocketAsyncEventArgs
		/// </summary>
		/// <returns></returns>
		private SocketAsyncEventArgs CreateNew()
		{
			SocketAsyncEventArgs item = new SocketAsyncEventArgs();
			item.DisconnectReuseSocket = true;
			var buffer = bufferManager.TakeBuffer( singleMaxBufferSize );
			item.SetBuffer( buffer, 0, buffer.Length );

			if ( completed != null )
			{
				item.Completed += completed;
			}

			return item;
		}
	}
}
