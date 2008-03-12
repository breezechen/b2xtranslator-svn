/*
 * Copyright (c) 2008, DIaLOGIKa
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *        notice, this list of conditions and the following disclaimer.
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
using DIaLOGIKa.b2xtranslator.StructuredStorageReader;
using DIaLOGIKa.b2xtranslator.CommonTranslatorLib;

namespace DIaLOGIKa.b2xtranslator.DocFileFormat
{
    public class WordDocument : IVisitable
    {
        private StorageReader _reader;
        private PieceTable _pieceTable;

        /// <summary>
        /// The stream "WordDocument"
        /// </summary>
        public VirtualStream WordDocumentStream;

        /// <summary>
        /// The stream "0Table" or "1Table"
        /// </summary>
        public VirtualStream TableStream;

        /// <summary>
        /// The file information block of the word document
        /// </summary>
        public FileInformationBlock FIB;

        /// <summary>
        /// The text part of the Word document
        /// </summary>
        public List<char> Text;

        /// <summary>
        /// The macros of the Word document
        /// </summary>
        public List<char> Macros;

        /// <summary>
        /// The headers of the Word document
        /// </summary>
        public List<char> Headers;

        /// <summary>
        /// The textboxes of the Word document
        /// </summary>
        public List<char> Textboxes;

        /// <summary>
        /// The annotations of the Word document
        /// </summary>
        public List<char> Annotations;

        /// <summary>
        /// The endnotes of the Word document
        /// </summary>
        public List<char> Endnotes;

        /// <summary>
        /// The footnotes of the Word document
        /// </summary>
        public List<char> Footnotes;

        /// <summary>
        /// The textboxes in headers of the Word document
        /// </summary>
        public List<char> HeaderTextboxes;

        /// <summary>
        /// The style sheet of the document
        /// </summary>
        public StyleSheet Styles;

        /// <summary>
        /// A list of all font names, used in the doucument
        /// </summary>
        public List<FontFamilyName> FontTable;

        public WordDocument(StorageReader reader)
        {
            this.WordDocumentStream = reader.GetStream("WordDocument");

            //parse FIB
            this.FIB = new FileInformationBlock(this.WordDocumentStream);

            //get the table stream
            if (this.FIB.fWhichTblStm)
                this.TableStream = reader.GetStream("1Table");
            else
                this.TableStream = reader.GetStream("0Table");

            //parse the stylesheet
            this.Styles = new StyleSheet(this.FIB, this.TableStream);

            //read font table
            this.FontTable = new List<FontFamilyName>();
            byte[] ftBytes = new byte[this.FIB.lcbSttbfffn];
            this.TableStream.Read(ftBytes, ftBytes.Length, this.FIB.fcSttbfffn);

            //parse the font table
            Int32 ffnCount = System.BitConverter.ToInt32(ftBytes, 0);
            int pos = 4;
            for (int i = 0; i < ffnCount; i++)
            {
                byte[] ffnBytes = new byte[ftBytes[pos]+1];
                Array.Copy(ftBytes, pos, ffnBytes, 0, ffnBytes.Length);
                this.FontTable.Add(new FontFamilyName(ffnBytes));
                pos += ffnBytes.Length ;
            }

            //parse the piece table and construct a list that contains all chars
            _pieceTable = new PieceTable(this.FIB, this.TableStream);
            List<char> allChars = new List<char>();
            for(int i=0; i<_pieceTable.Pieces.Count; i++)
            {
                PieceDescriptor pcd = _pieceTable.Pieces[i];
                int cb = 0;

                //calculate the count of bytes
                if (i != (_pieceTable.Pieces.Count - 1))
                {
                    //use the begin of the next piece
                    PieceDescriptor pcdNext = _pieceTable.Pieces[i + 1];
                    cb = (Int32)pcdNext.fc - (Int32)pcd.fc;
                }
                else
                {
                    //for the last piece, use the fib.fcMac
                    cb = (Int32)FIB.fcMac - (Int32)pcd.fc;
                }

                //read the bytes of that piece
                byte[] bytesOfPiece = new byte[cb];
                this.WordDocumentStream.Read(bytesOfPiece, cb, (Int32)pcd.fc);

                //encode it
                char[] chars = pcd.encoding.GetString(bytesOfPiece).ToCharArray();
                
                //append it
                foreach(char c in chars)
                {
                    allChars.Add(c);
                }
            }

            //split the chars into the subdocuments
            this.Text = allChars.GetRange(0, FIB.ccpText);
            this.Footnotes = allChars.GetRange(FIB.ccpText, FIB.ccpFtn);
            this.Headers = allChars.GetRange(FIB.ccpText + FIB.ccpFtn, FIB.ccpHdr);
            this.Annotations = allChars.GetRange(FIB.ccpText + FIB.ccpFtn + FIB.ccpHdr, FIB.ccpAtn);
            this.Endnotes = allChars.GetRange(FIB.ccpText + FIB.ccpFtn + FIB.ccpHdr + FIB.ccpAtn, FIB.ccpEdn);
            this.Textboxes = allChars.GetRange(FIB.ccpText + FIB.ccpFtn + FIB.ccpHdr + FIB.ccpAtn + FIB.ccpEdn, FIB.ccpTxbx);
            this.HeaderTextboxes = allChars.GetRange(FIB.ccpText + FIB.ccpFtn + FIB.ccpHdr + FIB.ccpAtn + FIB.ccpEdn + FIB.ccpTxbx, FIB.ccpHdrTxbx);
        }

        #region IVisitable Members

        public void Convert<T>(T mapping)
        {
            ((IMapping<WordDocument>)mapping).Apply(this);
        }

        #endregion
    }
}
