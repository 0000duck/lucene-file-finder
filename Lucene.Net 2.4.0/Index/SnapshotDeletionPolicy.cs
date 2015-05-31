/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Generic;

using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Index
{
	
	/// <summary>A {@link IndexDeletionPolicy} that wraps around any other
	/// {@link IndexDeletionPolicy} and adds the ability to hold and
	/// later release a single "snapshot" of an index.  While
	/// the snapshot is held, the {@link IndexWriter} will not
	/// remove any files associated with it even if the index is
	/// otherwise being actively, arbitrarily changed.  Because
	/// we wrap another arbitrary {@link IndexDeletionPolicy}, this
	/// gives you the freedom to continue using whatever {@link
	/// IndexDeletionPolicy} you would normally want to use with your
    /// index.  Note that you can re-use a single instance of
    /// SnapshotDeletionPolicy across multiple writers as long
    /// as they are against the same index Directory.  Any 
    /// snapshot held when a writer is closed will "survive"
    /// when the next writer is opened.
    /// <para>
    /// WARNING: This API is new and experimental and may suddnely changendex. 
    /// </para>
	/// </summary>
	
	public class SnapshotDeletionPolicy : IndexDeletionPolicy
	{
		
		private IndexCommit lastCommit;
		private IndexDeletionPolicy primary;
		private string snapshot;
		
		public SnapshotDeletionPolicy(IndexDeletionPolicy primary)
		{
			this.primary = primary;
		}

        public virtual void OnInit(List<IndexCommitPoint> commits)
		{
			lock (this)
			{
				primary.OnInit(WrapCommits(commits));
				lastCommit = (IndexCommit)(commits[commits.Count - 1]);
			}
		}

        public virtual void OnCommit(List<IndexCommitPoint> commits)
		{
			lock (this)
			{
				primary.OnCommit(WrapCommits(commits));
                lastCommit = (IndexCommit)(commits[commits.Count - 1]);
			}
		}
		
		/// <summary>Take a snapshot of the most recent commit to the
		/// index.  You must call release() to free this snapshot.
		/// Note that while the snapshot is held, the files it
		/// references will not be deleted, which will consume
		/// additional disk space in your index. If you take a
		/// snapshot at a particularly bad time (say just before
		/// you call optimize()) then in the worst case this could
		/// consume an extra 1X of your total index size, until
		/// you release the snapshot. 
		/// </summary>
        /// // TODO: 3.9: change this to return IndexCommit instead
		public virtual IndexCommitPoint Snapshot()
		{
			lock (this)
			{
				if (snapshot == null)
					snapshot = lastCommit.GetSegmentsFileName();
				else
					throw new System.SystemException("snapshot is already set; please call release() first");
				return lastCommit;
			}
		}
		
		/// <summary>Release the currently held snapshot. </summary>
		public virtual void  Release()
		{
			lock (this)
			{
				if (snapshot != null)
					snapshot = null;
				else
					throw new System.SystemException("snapshot was not set; please call snapshot() first");
			}
		}
		
		private class MyCommitPoint : IndexCommitPoint
		{
			private void  InitBlock(SnapshotDeletionPolicy enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private SnapshotDeletionPolicy enclosingInstance;
			public SnapshotDeletionPolicy Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			internal IndexCommit cp;
			internal MyCommitPoint(SnapshotDeletionPolicy enclosingInstance, IndexCommit cp)
			{
				InitBlock(enclosingInstance);
				this.cp = cp;
			}
			public virtual System.String GetSegmentsFileName()
			{
				return cp.GetSegmentsFileName();
			}
			public virtual System.Collections.Generic.ICollection<string> GetFileNames()
			{
				return cp.GetFileNames();
			}
            public Directory GetDirectory()
            {
                return cp.GetDirectory();
            }
			public virtual void  Delete()
			{
				lock (Enclosing_Instance)
				{
					// Suppress the delete request if this commit point is
					// our current snapshot.
					if (Enclosing_Instance.snapshot == null || !Enclosing_Instance.snapshot.Equals(GetSegmentsFileName()))
						cp.Delete();
				}
			}
            public bool IsDeleted() { return cp.IsDeleted(); }
            public int GetVersion() { return (int) cp.GetVersion(); }
            public int GetGeneration() { return (int) cp.GetGeneration(); }

		}

        private List<IndexCommitPoint> WrapCommits(List<IndexCommitPoint> commits)
		{
			int count = commits.Count;
            List<IndexCommitPoint> myCommits = new List<IndexCommitPoint>(count);
			for (int i = 0; i < count; i++)
				myCommits.Add(new MyCommitPoint(this, (IndexCommit) commits[i]));
			return myCommits;
		}
	}
}