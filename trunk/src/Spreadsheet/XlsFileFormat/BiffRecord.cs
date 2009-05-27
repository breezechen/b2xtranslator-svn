/*
 * Copyright (c) 2008, DIaLOGIKa
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of DIaLOGIKa nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY DIaLOGIKa ''AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL DIaLOGIKa BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
using System;
using System.Collections.Generic;
using System.Text;
using DIaLOGIKa.b2xtranslator.StructuredStorage.Reader;
using System.Diagnostics;
using System.Reflection;

namespace DIaLOGIKa.b2xtranslator.Spreadsheet.XlsFileFormat
{
    public abstract class BiffRecord
    {
        IStreamReader _reader;
        
        RecordNumber _id;
        UInt32 _length;
        long _offset;

        /// <summary>
        /// Ctor 
        /// </summary>
        /// <param name="reader">Streamreader</param>
        /// <param name="id">Record ID - Recordtype</param>
        /// <param name="length">The recordlegth</param>
        public BiffRecord(IStreamReader reader, RecordNumber id, UInt32 length)
        {
            _reader = reader;
            _offset = _reader.BaseStream.Position;

            _id = id;
            _length = length;
        }

        private static Dictionary<UInt16, Type> TypeToRecordClassMapping = new Dictionary<UInt16, Type>();

        static BiffRecord()
        {
            UpdateTypeToRecordClassMapping(
                Assembly.GetExecutingAssembly(),
                typeof(BiffRecord).Namespace);
        }

        public static void UpdateTypeToRecordClassMapping(Assembly assembly, String ns)
        {
            foreach (Type t in assembly.GetTypes())
            {
                if (ns == null || t.Namespace == ns)
                {
                    object[] attrs = t.GetCustomAttributes(typeof(BiffRecordAttribute), false);

                    BiffRecordAttribute attr = null;

                    if (attrs.Length > 0)
                        attr = attrs[0] as BiffRecordAttribute;

                    if (attr != null)
                    {
                        // Add the type codes of the array
                        foreach (UInt16 typeCode in attr.TypeCodes)
                        {
                            if (TypeToRecordClassMapping.ContainsKey(typeCode))
                            {
                                throw new Exception(String.Format(
                                    "Tried to register TypeCode {0} to {1}, but it is already registered to {2}",
                                    typeCode, t, TypeToRecordClassMapping[typeCode]));
                            }
                            TypeToRecordClassMapping.Add(typeCode, t);
                        }
                    }
                }
            }
        }

        public static BiffRecord ReadRecord(IStreamReader reader)
        {
            BiffRecord result = null;
            try
            {
                UInt16 id = reader.ReadUInt16();
                UInt16 size = reader.ReadUInt16();
                Type cls;
                if (TypeToRecordClassMapping.TryGetValue(id, out cls))
                {
                    ConstructorInfo constructor = cls.GetConstructor(
                        new Type[] { typeof(IStreamReader), typeof(RecordNumber), typeof(UInt16) }
                        );

                    try
                    {
                        result = (BiffRecord)constructor.Invoke(
                            new object[] { reader, id, size }
                            );
                    }
                    catch (TargetInvocationException e)
                    {
                        throw e.InnerException;
                    }
                }
                else
                {
                    result = new UnknownBiffRecord(reader, (RecordNumber)id, size);
                }

                return result;
            }
            catch (OutOfMemoryException e)
            {
                throw new Exception("Invalid BIFF record", e);
            }
        }

        public RecordNumber Id
        {
            get { return _id; }
        }
        
        public UInt32 Length
        {
            get { return _length; }
        }

        public long Offset
        {
            get { return _offset; }
        }

        public IStreamReader Reader
        {
            get { return _reader; }
            set { this._reader = value; }
        }


    }
}
