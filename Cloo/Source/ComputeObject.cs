﻿/*

Copyright (c) 2009 Fatjon Sakiqi

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using OpenTK.Compute.CL10;
using System.Globalization;

namespace Cloo
{
    public abstract class ComputeObject: IEquatable<ComputeObject>
    {
        private IntPtr handle;

        public IntPtr Handle
        {
            get
            {
                return handle;
            }
            protected set
            {
                handle = value;
            }
        }

        public override int GetHashCode()
        {
            return Handle.GetHashCode();
        }

        public new static bool Equals( object objA, object objB )
        {
            if( objA == objB ) return true;
            if( objA == null || objB == null ) return false;
            return objA.Equals( objB );
        }

        public override bool Equals( object obj )
        {
            if( obj == null ) return false;
            if( !( obj is ComputeObject ) ) return false;
            return Equals( obj as ComputeObject );
        }

        public bool Equals( ComputeObject obj )
        {
            if( obj == null ) return false;
            if( !Handle.Equals( obj.Handle ) ) return false;
            return true;
        }

        public override string ToString()
        {
            return "(" + Handle.ToString() + ")";
        }

        internal static IntPtr[] ExtractHandles<T>( ICollection<T> computeObjects ) where T: ComputeObject
        {
            IntPtr[] result = new IntPtr[ computeObjects.Count ];
            int i = 0;
            foreach( T computeObj in computeObjects )
            {
                result[ i ] = computeObj.Handle;
                i++;
            }
            return result;
        }

        protected ResultType[] GetArrayInfo<InfoType, ResultType>
            (
                InfoType paramName,
                GetInfoDelegate<InfoType> getInfoDelegate
            )
        {
            int errorCode;
            ResultType[] buffer;
            IntPtr bufferSizeRet;
            getInfoDelegate( handle, paramName, IntPtr.Zero, IntPtr.Zero, out bufferSizeRet );
            buffer = new ResultType[ bufferSizeRet.ToInt64() / Marshal.SizeOf( typeof( ResultType ) ) ];
            GCHandle gcHandle = GCHandle.Alloc( buffer, GCHandleType.Pinned );
            try
            {
                errorCode = getInfoDelegate(
                    handle,
                    paramName,
                    bufferSizeRet,
                    gcHandle.AddrOfPinnedObject(),
                    out bufferSizeRet );
            }
            finally
            {
                gcHandle.Free();
            }
            ComputeException.ThrowIfError( errorCode );
            return buffer;
        }

        protected ResultType[] GetArrayInfo<InfoType, ResultType>
            (
                ComputeObject secondaryObject,
                InfoType paramName,
                GetInfoDelegateEx<InfoType> getInfoDelegate
            )
        {
            int errorCode;
            ResultType[] buffer;
            IntPtr bufferSizeRet;
            getInfoDelegate( handle, secondaryObject.handle, paramName, IntPtr.Zero, IntPtr.Zero, out bufferSizeRet );
            buffer = new ResultType[ bufferSizeRet.ToInt64() / Marshal.SizeOf( typeof( ResultType ) ) ];
            GCHandle gcHandle = GCHandle.Alloc( buffer, GCHandleType.Pinned );
            try
            {
                errorCode = getInfoDelegate(
                    handle,
                    secondaryObject.handle,
                    paramName,
                    bufferSizeRet,
                    gcHandle.AddrOfPinnedObject(),
                    out bufferSizeRet );
            }
            finally
            {
                gcHandle.Free();
            }
            ComputeException.ThrowIfError( errorCode );
            return buffer;
        }

        protected bool GetBoolInfo<InfoType>
            (
                InfoType paramName,
                GetInfoDelegate<InfoType> getInfoDelegate
            )
        {
            int result = GetInfo<InfoType, int, int>
                ( paramName, getInfoDelegate );
            return ( result == 0 ) ? false : true;
        }

        protected ResultType GetInfo<InfoType, ResultType, NativeType>
            (
                InfoType paramName,
                GetInfoDelegate<InfoType> getInfoDelegate
            )
            where ResultType: struct
            where NativeType: struct             
        {
            IntPtr valueSizeRet;
            NativeType nativeResult = new NativeType();
            ResultType result;
            GCHandle gcHandle = GCHandle.Alloc( nativeResult, GCHandleType.Pinned );
            int errorCode;
            try
            {                
                errorCode = getInfoDelegate(
                    handle,
                    paramName,
                    ( IntPtr )Marshal.SizeOf( nativeResult ),
                    gcHandle.AddrOfPinnedObject(),
                    out valueSizeRet );                
            }
            finally
            {
                result = ( ResultType )Convert.ChangeType( gcHandle.Target, typeof( ResultType ), new NumberFormatInfo() );
                gcHandle.Free();                
            }
            ComputeException.ThrowIfError( errorCode );
            return result;
        }

        protected ResultType GetInfo<InfoType, ResultType, NativeType>
            (
                ComputeObject secondaryObject,
                InfoType paramName,
                GetInfoDelegateEx<InfoType> getInfoDelegate
            )
            where ResultType : struct
            where NativeType : struct
        {
            IntPtr valueSizeRet;
            NativeType nativeResult = new NativeType();
            ResultType result;
            GCHandle gcHandle = GCHandle.Alloc( nativeResult, GCHandleType.Pinned );
            int errorCode;
            try
            {
                errorCode = getInfoDelegate(
                    handle,
                    secondaryObject.handle,
                    paramName,
                    ( IntPtr )Marshal.SizeOf( nativeResult ),
                    gcHandle.AddrOfPinnedObject(),
                    out valueSizeRet );
            }
            finally
            {
                result = ( ResultType )Convert.ChangeType( gcHandle.Target, typeof( ResultType ), new NumberFormatInfo() );
                gcHandle.Free();
            }
            ComputeException.ThrowIfError( errorCode );
            return result;
        }

        protected string GetStringInfo<InfoType>( InfoType paramName, GetInfoDelegate<InfoType> getInfoDelegate )
        {
            string result = null;
            sbyte[] buffer = GetArrayInfo<InfoType, sbyte>( paramName, getInfoDelegate );
            unsafe
            {
                fixed( sbyte* bufferPtr = buffer )
                    result = new string( bufferPtr );
            }
            return result.Trim();
        }

        protected string GetStringInfo<InfoType>( ComputeObject secondaryObject, InfoType paramName, GetInfoDelegateEx<InfoType> getInfoDelegate )
        {
            string result = null;
            sbyte[] buffer = GetArrayInfo<InfoType, sbyte>( secondaryObject, paramName, getInfoDelegate );
            unsafe
            {
                fixed( sbyte* bufferPtr = buffer )
                    result = new string( bufferPtr );
            }
            return result.Trim();
        }

        protected unsafe delegate int GetInfoDelegate<InfoType>
            (
                IntPtr objectHandle,
                InfoType paramName,
                IntPtr paramValueSize,
                IntPtr paramValue,
                out IntPtr paramValueSizeRet
            );

        protected unsafe delegate int GetInfoDelegateEx<InfoType>
            (
                IntPtr mainObjectHandle,
                IntPtr secondaryObjectHandle,
                InfoType paramName,
                IntPtr paramValueSize,
                IntPtr paramValue,
                out IntPtr paramValueSizeRet
            );
    }
}