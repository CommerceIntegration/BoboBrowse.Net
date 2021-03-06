﻿//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
//*
//* Copyright (C) 2005-2015  John Wang
//*
//* Licensed under the Apache License, Version 2.0 (the "License");
//* you may not use this file except in compliance with the License.
//* You may obtain a copy of the License at
//*
//*   http://www.apache.org/licenses/LICENSE-2.0
//*
//* Unless required by applicable law or agreed to in writing, software
//* distributed under the License is distributed on an "AS IS" BASIS,
//* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//* See the License for the specific language governing permissions and
//* limitations under the License.

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// author "Xiaoyang Gu &lt;xgu@linkedin.com&gt;"
    /// <para/>
    /// NOTE: This was CombinedFloatFacetIterator in bobo-browse
    /// </summary>
    public class CombinedSingleFacetIterator : SingleFacetIterator
    {
        /// <summary>
        /// NOTE: This was FloatIteratorNode in bobo-browse
        /// </summary>
        public class SingleIteratorNode
        {
            private readonly SingleFacetIterator m_iterator;
            protected float m_curFacet;
            protected int m_curFacetCount;

            public SingleIteratorNode(SingleFacetIterator iterator)
            {
                m_iterator = iterator;
                m_curFacet = TermSingleList.VALUE_MISSING;
                m_curFacetCount = 0;
            }

            /// <summary>
            /// Added in .NET version as an accessor to the _iterator field.
            /// </summary>
            /// <returns></returns>
            public virtual SingleFacetIterator GetIterator()
            {
                return m_iterator;
            }

            /// <summary>
            /// Added in .NET version as an accessor to the _curFacet field.
            /// </summary>
            public virtual float CurFacet
            {
                get { return m_curFacet; }
            }

            /// <summary>
            /// Added in .NET version as an accessor to the _curFacetCount field.
            /// </summary>
            public virtual int CurFacetCount
            {
                get { return m_curFacetCount; }
            }

            public virtual bool Fetch(int minHits)
            {
                if (minHits > 0)
                    minHits = 1;
                if ((m_curFacet = m_iterator.NextSingle(minHits)) != TermSingleList.VALUE_MISSING)
                {
                    m_curFacetCount = m_iterator.Count;
                    return true;
                }
                m_curFacet = TermSingleList.VALUE_MISSING;
                m_curFacetCount = 0;
                return false;
            }
        }

        private readonly SingleFacetPriorityQueue m_queue;

        private IList<SingleFacetIterator> m_iterators;

        private CombinedSingleFacetIterator(int length)
        {
            m_queue = new SingleFacetPriorityQueue();
            m_queue.Initialize(length);           
        }

        public CombinedSingleFacetIterator(IList<SingleFacetIterator> iterators)
            : this(iterators.Count)
        {
            m_iterators = iterators;
            foreach (SingleFacetIterator iterator in iterators)
            {
                SingleIteratorNode node = new SingleIteratorNode(iterator);
                if (node.Fetch(1))
                    m_queue.Add(node);
            }
            m_facet = TermSingleList.VALUE_MISSING;
            m_count = 0;
        }

        public CombinedSingleFacetIterator(IList<SingleFacetIterator> iterators, int minHits)
            : this(iterators.Count)
        {
            m_iterators = iterators;
            foreach (SingleFacetIterator iterator in iterators)
            {
                SingleIteratorNode node = new SingleIteratorNode(iterator);
                if (node.Fetch(minHits))
                    m_queue.Add(node);
            }
            m_facet = TermSingleList.VALUE_MISSING;
            m_count = 0;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.FacetIterator#getFacet()
        /// </summary>
        /// <returns></returns>
        public virtual string GetFacet()
        {
            if (m_facet == TermSingleList.VALUE_MISSING) return null;
            return Format(m_facet);
        }

        public override string Format(float val)
        {
            return m_iterators[0].Format(val);
        }

        public override string Format(object val)
        {
            return m_iterators[0].Format(val);
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.FacetIterator#getFacetCount()
        /// </summary>
        /// <returns></returns>
        public virtual int FacetCount
        {
            get { return m_count; }
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.FacetIterator#next()
        /// </summary>
        /// <returns></returns>
        public override string Next()
        {
            if (!HasNext())
                throw new IndexOutOfRangeException("No more facets in this iteration");

            SingleIteratorNode node = m_queue.Top;

            m_facet = node.CurFacet;
            float next = TermSingleList.VALUE_MISSING;
            m_count = 0;
            while (HasNext())
            {
                node = m_queue.Top;
                next = node.CurFacet;
                if ((next != TermSingleList.VALUE_MISSING) && (next != m_facet))
                {
                    return Format(m_facet);
                }
                m_count += node.CurFacetCount;
                if (node.Fetch(1))
                    m_queue.UpdateTop();
                else
                    m_queue.Pop();
            }
            return null;
        }

        /// <summary>
        /// This version of the next() method applies the minHits from the facet spec
        /// before returning the facet and its hitcount
        /// </summary>
        /// <param name="minHits">the minHits from the facet spec for CombinedFacetAccessible</param>
        /// <returns>The next facet that obeys the minHits</returns>
        public override string Next(int minHits)
        {
            int qsize = m_queue.Count;
            if (qsize == 0)
            {
                m_facet = TermSingleList.VALUE_MISSING;
                m_count = 0;
                return null;
            }

            SingleIteratorNode node = m_queue.Top;
            m_facet = node.CurFacet;
            m_count = node.CurFacetCount;
            while (true)
            {
                if (node.Fetch(minHits))
                {
                    node = m_queue.UpdateTop();
                }
                else
                {
                    m_queue.Pop();
                    if (--qsize > 0)
                    {
                        node = m_queue.Top;
                    }
                    else
                    {
                        // we reached the end. check if this facet obeys the minHits
                        if (m_count < minHits)
                        {
                            m_facet = TermSingleList.VALUE_MISSING;
                            m_count = 0;
                            return null;
                        }
                        break;
                    }
                }
                float next = node.CurFacet;
                if (next != m_facet)
                {
                    // check if this facet obeys the minHits
                    if (m_count >= minHits)
                        break;
                    // else, continue iterating to the next facet
                    m_facet = next;
                    m_count = node.CurFacetCount;
                }
                else
                {
                    m_count += node.CurFacetCount;
                }
            }
            return Format(m_facet);
        }

        /// <summary>
        /// (non-Javadoc)
        /// see java.util.Iterator#hasNext()
        /// </summary>
        /// <returns></returns>
        public override bool HasNext()
        {
            return (m_queue.Count > 0);
        }

        // BoboBrowse.Net: Not supported in .NET anyway
        ///// <summary>
        ///// (non-Javadoc)
        ///// see java.util.Iterator#remove()
        ///// </summary>
        //public override void Remove()
        //{
        //    throw new NotSupportedException("remove() method not supported for Facet Iterators");
        //}

        /// <summary>
        /// Lucene PriorityQueue
        /// <para/>
        /// NOTE: This was FloatFacetPriorityQueue in bobo-browse
        /// </summary>
        public class SingleFacetPriorityQueue
        {
            private int m_size;
            private int m_maxSize;
            protected SingleIteratorNode[] m_heap;

            /// <summary>
            /// Subclass constructors must call this.
            /// </summary>
            /// <param name="maxSize"></param>
            public void Initialize(int maxSize)
            {
                m_size = 0;
                int heapSize;
                if (0 == maxSize)
                    // We allocate 1 extra to avoid if statement in top()
                    heapSize = 2;
                else
                    heapSize = maxSize + 1;
                m_heap = new SingleIteratorNode[heapSize];
                this.m_maxSize = maxSize;
            }

            public void Put(SingleIteratorNode element)
            {
                m_size++;
                m_heap[m_size] = element;
                UpHeap();
            }

            public SingleIteratorNode Add(SingleIteratorNode element)
            {
                m_size++;
                m_heap[m_size] = element;
                UpHeap();
                return m_heap[1];
            }

            public virtual bool Insert(SingleIteratorNode element)
            {
                return InsertWithOverflow(element) != element;
            }

            public virtual SingleIteratorNode InsertWithOverflow(SingleIteratorNode element)
            {
                if (m_size < m_maxSize)
                {
                    Put(element);
                    return null;
                }
                else if (m_size > 0 && !(element.CurFacet < m_heap[1].CurFacet))
                {
                    SingleIteratorNode ret = m_heap[1];
                    m_heap[1] = element;
                    AdjustTop();
                    return ret;
                }
                else
                {
                    return element;
                }
            }

            /// <summary>
            /// Returns the least element of the PriorityQueue in constant time.
            /// </summary>
            /// <returns></returns>
            public SingleIteratorNode Top
            {
                get
                {
                    // We don't need to check size here: if maxSize is 0,
                    // then heap is length 2 array with both entries null.
                    // If size is 0 then heap[1] is already null.
                    return m_heap[1];
                }
            }

            /// <summary>
            /// Removes and returns the least element of the PriorityQueue in 
            /// log(size) time.
            /// </summary>
            /// <returns></returns>
            public SingleIteratorNode Pop()
            {
                if (m_size > 0)
                {
                    SingleIteratorNode result = m_heap[1]; // save first value
                    m_heap[1] = m_heap[m_size]; // move last to first
                    m_heap[m_size] = null; // permit GC of objects
                    m_size--;
                    DownHeap(); // adjust heap
                    return result;
                }
                else
                    return null;
            }

            public void AdjustTop()
            {
                DownHeap();
            }

            public SingleIteratorNode UpdateTop()
            {
                DownHeap();
                return m_heap[1];
            }

            /// <summary>
            /// Returns the number of elements currently stored in the PriorityQueue.
            /// </summary>
            /// <returns></returns>
            // BoboBrowse.Net: we use Count instead of Size() in .NET
            public int Count
            {
                get { return m_size; }
            }

            /// <summary>
            /// Removes all entries from the PriorityQueue.
            /// </summary>
            public void Clear()
            {
                for (int i = 0; i <= m_size; i++)
                {
                    m_heap[i] = null;
                }
                m_size = 0;
            }

            private void UpHeap()
            {
                int i = m_size;
                SingleIteratorNode node = m_heap[i]; // save bottom node
                int j = (int)(((uint)i) >> 1);
                while (j > 0 && (node.CurFacet < m_heap[j].CurFacet))
                {
                    m_heap[i] = m_heap[j]; // shift parents down
                    i = j;
                    j = (int)(((uint)j) >> 1);
                }
                m_heap[i] = node; // install saved node
            }

            private void DownHeap()
            {
                int i = 1;
                SingleIteratorNode node = m_heap[i]; // save top node
                int j = i << 1; // find smaller child
                int k = j + 1;
                if (k <= m_size && (m_heap[k].CurFacet < m_heap[j].CurFacet))
                {
                    j = k;
                }
                while (j <= m_size && (m_heap[j].CurFacet < node.CurFacet))
                {
                    m_heap[i] = m_heap[j]; // shift up child
                    i = j;
                    j = i << 1;
                    k = j + 1;
                    if (k <= m_size && (m_heap[k].CurFacet < m_heap[j].CurFacet))
                    {
                        j = k;
                    }
                }
                m_heap[i] = node; // install saved node
            }
        }

        public override float NextSingle()
        {
            if (!HasNext())
                throw new IndexOutOfRangeException("No more facets in this iteration");

            SingleIteratorNode node = m_queue.Top;

            m_facet = node.CurFacet;
            float next = TermSingleList.VALUE_MISSING;
            m_count = 0;
            while (HasNext())
            {
                node = m_queue.Top;
                next = node.CurFacet;
                if ((next != TermSingleList.VALUE_MISSING) && (next != m_facet))
                {
                    return m_facet;
                }
                m_count += node.CurFacetCount;
                if (node.Fetch(1))
                    m_queue.UpdateTop();
                else
                    m_queue.Pop();
            }
            return TermSingleList.VALUE_MISSING;
        }

        public override float NextSingle(int minHits)
        {
            int qsize = m_queue.Count;
            if (qsize == 0)
            {
                m_facet = TermSingleList.VALUE_MISSING;
                m_count = 0;
                return TermSingleList.VALUE_MISSING;
            }

            SingleIteratorNode node = m_queue.Top;
            m_facet = node.CurFacet;
            m_count = node.CurFacetCount;
            while (true)
            {
                if (node.Fetch(minHits))
                {
                    node = m_queue.UpdateTop();
                }
                else
                {
                    m_queue.Pop();
                    if (--qsize > 0)
                    {
                        node = m_queue.Top;
                    }
                    else
                    {
                        // we reached the end. check if this facet obeys the minHits
                        if (m_count < minHits)
                        {
                            m_facet = TermSingleList.VALUE_MISSING;
                            m_count = 0;
                        }
                        break;
                    }
                }
                float next = node.CurFacet;
                if (next != m_facet)
                {
                    // check if this facet obeys the minHits
                    if (m_count >= minHits)
                        break;
                    // else, continue iterating to the next facet
                    m_facet = next;
                    m_count = node.CurFacetCount;
                }
                else
                {
                    m_count += node.CurFacetCount;
                }
            }
            return m_facet;
        }
    }
}
