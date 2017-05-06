﻿//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
//*
//* Copyright (C) 2015  Shad Storhaug
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

namespace BoboBrowse.Net.Support
{
    using System.Threading;

    public class AtomicInt64
    {
        private long _value = 0;

        public AtomicInt64()
        {
        }

        public AtomicInt64(long value)
        {
            _value = value;
        }

        public long IncrementAndGet()
        {
            return Interlocked.Increment(ref _value);
        }

        public long GetAndAdd(long value)
        {
            return Interlocked.Add(ref _value, value);
        }

        public long Get()
        {
            return Interlocked.Read(ref _value);
        }
    }
}
