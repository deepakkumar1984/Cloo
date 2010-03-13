﻿#region License

/*

Copyright (c) 2009 - 2010 Fatjon Sakiqi

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

#endregion

namespace Cloo
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    public abstract class ComputeObject: IEquatable<ComputeObject>
    {
        #region Fields

        private IntPtr handle;
        
        #endregion

        #region Properties

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

        #endregion

        #region Public methods

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

        /// <summary>
        /// Gets the hash code for this compute object.
        /// </summary>
        public override int GetHashCode()
        {
            return Handle.GetHashCode();
        }

        /// <summary>
        /// Gets a string representation for this object.
        /// </summary>
        public override string ToString()
        {
            return "(" + Handle.ToString() + ")";
        }

        #endregion

        #region Protected methods

        protected QueriedType[] GetArrayInfo<InfoEnum, QueriedType>
            (
                InfoEnum paramName,
                GetInfoDelegate<InfoEnum> getInfoDelegate
            )
        {
            unsafe
            {
                ComputeErrorCode error;
                QueriedType[] buffer;
                IntPtr bufferSizeRet;
                getInfoDelegate( handle, paramName, IntPtr.Zero, IntPtr.Zero, &bufferSizeRet );
                buffer = new QueriedType[ bufferSizeRet.ToInt64() / Marshal.SizeOf( typeof( QueriedType ) ) ];
                GCHandle gcHandle = GCHandle.Alloc( buffer, GCHandleType.Pinned );
                try
                {
                    error = getInfoDelegate(
                        handle,
                        paramName,
                        bufferSizeRet,
                        gcHandle.AddrOfPinnedObject(),
                        null );
                    ComputeException.ThrowOnError( error );
                }
                finally
                {
                    gcHandle.Free();
                }
                return buffer;
            }
        }

        protected QueriedType[] GetArrayInfo<InfoEnum, QueriedType>
            (
                ComputeObject secondaryObject,
                InfoEnum paramName,
                GetInfoDelegateEx<InfoEnum> getInfoDelegate
            )
        {
            unsafe
            {
                ComputeErrorCode error;
                QueriedType[] buffer;
                IntPtr bufferSizeRet;
                error = getInfoDelegate( handle, secondaryObject.handle, paramName, IntPtr.Zero, IntPtr.Zero, &bufferSizeRet );
                buffer = new QueriedType[ bufferSizeRet.ToInt64() / Marshal.SizeOf( typeof( QueriedType ) ) ];
                GCHandle gcHandle = GCHandle.Alloc( buffer, GCHandleType.Pinned );
                try
                {
                    error = getInfoDelegate(
                        handle,
                        secondaryObject.handle,
                        paramName,
                        bufferSizeRet,
                        gcHandle.AddrOfPinnedObject(),
                        null );
                    ComputeException.ThrowOnError( error );
                }
                finally
                {
                    gcHandle.Free();
                }
                return buffer;
            }
        }

        protected bool GetBoolInfo<InfoEnum>
            (
                InfoEnum paramName,
                GetInfoDelegate<InfoEnum> getInfoDelegate
            )
        {
            int result = GetInfo<InfoEnum, int>( paramName, getInfoDelegate );
            return ( result == ( int )ComputeBoolean.True ) ? true : false;
        }

        protected QueriedType GetInfo<InfoEnum, QueriedType>
            (
                InfoEnum paramName,
                GetInfoDelegate<InfoEnum> getInfoDelegate
            )
            where QueriedType: struct             
        {
            unsafe
            {
                ComputeErrorCode error;
                QueriedType result = new QueriedType();
                GCHandle gcHandle = GCHandle.Alloc( result, GCHandleType.Pinned );
                try
                {
                    error = getInfoDelegate(
                        handle,
                        paramName,
                        ( IntPtr )Marshal.SizeOf( result ),
                        gcHandle.AddrOfPinnedObject(),
                        null );
                    ComputeException.ThrowOnError( error );
                }
                finally
                {
                    result = ( QueriedType )gcHandle.Target;
                    gcHandle.Free();
                }
                return result;
            }
        }

        protected QueriedType GetInfo<InfoEnum, QueriedType>
            (
                ComputeObject secondaryObject,
                InfoEnum paramName,
                GetInfoDelegateEx<InfoEnum> getInfoDelegate
            )
            where QueriedType : struct
        {
            unsafe
            {
                QueriedType result = new QueriedType();
                GCHandle gcHandle = GCHandle.Alloc( result, GCHandleType.Pinned );
                try
                {
                    ComputeErrorCode error = getInfoDelegate(
                        handle,
                        secondaryObject.handle,
                        paramName,
                        new IntPtr( Marshal.SizeOf( result ) ),
                        gcHandle.AddrOfPinnedObject(),
                        null );
                    ComputeException.ThrowOnError( error );
                }
                finally
                {
                    result = ( QueriedType )gcHandle.Target;
                    gcHandle.Free();
                }

                return result;
            }
        }

        protected string GetStringInfo<InfoEnum>( InfoEnum paramName, GetInfoDelegate<InfoEnum> getInfoDelegate )
        {
            unsafe
            {
                string result = null;
                sbyte[] buffer = GetArrayInfo<InfoEnum, sbyte>( paramName, getInfoDelegate );
                fixed( sbyte* bufferPtr = buffer )
                    result = new string( bufferPtr );
                return result;
            }
        }

        protected string GetStringInfo<InfoEnum>( ComputeObject secondaryObject, InfoEnum paramName, GetInfoDelegateEx<InfoEnum> getInfoDelegate )
        {
            unsafe
            {
                string result = null;
                sbyte[] buffer = GetArrayInfo<InfoEnum, sbyte>( secondaryObject, paramName, getInfoDelegate );
                fixed( sbyte* bufferPtr = buffer )
                    result = new string( bufferPtr );

                return result;
            }
        }

        #endregion

        #region Delegates

        protected unsafe delegate ComputeErrorCode GetInfoDelegate<InfoEnum>
            (
                IntPtr objectHandle,
                InfoEnum paramName,
                IntPtr paramValueSize,
                IntPtr paramValue,
                IntPtr* paramValueSizeRet
            );

        protected unsafe delegate ComputeErrorCode GetInfoDelegateEx<InfoEnum>
            (
                IntPtr mainObjectHandle,
                IntPtr secondaryObjectHandle,
                InfoEnum paramName,
                IntPtr paramValueSize,
                IntPtr paramValue,
                IntPtr* paramValueSizeRet
            );

        #endregion
    }
}