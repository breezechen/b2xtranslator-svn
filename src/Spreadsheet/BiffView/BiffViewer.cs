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
using DIaLOGIKa.b2xtranslator.StructuredStorageReader;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using DIaLOGIKa.b2xtranslator.Spreadsheet.XlsFileFormat;
using System.ComponentModel;

namespace DIaLOGIKa.b2xtranslator.Spreadsheet.BiffView
{
    public class BiffViewer
    {
        struct BiffHeader
        {
            public RecordNumber id;
            public UInt16 length;
        }
        
        private BiffViewerOptions _options;
        private BackgroundWorker _backgroundWorker;
        private bool _isCancelled = false;
        
        public BiffViewerOptions Options
        {
            get { return _options; }
            set { _options = value; }
        }
        
        public BiffViewer(BiffViewerOptions options)
        {
            this._options = options;
        }

        public void DoTheMagic(BackgroundWorker backgroundWorker)
        {
            _backgroundWorker = backgroundWorker;
            DoTheMagic();
            _backgroundWorker = null;
        }

        public void DoTheMagic()
        {
            StructuredStorageFile reader = null;
            StreamWriter sw = null;
            try
            {
                reader = new StructuredStorageFile(this.Options.InputDocument);
                IStreamReader workbookReader = new VirtualStreamReader(reader.GetStream("Workbook"));

                if (this.Options.Mode == BiffViewerMode.File)
                {
                    sw = File.CreateText(this.Options.OutputFileName);
                }
                else
                {
                    sw = new StreamWriter(Console.OpenStandardOutput());
                }
                sw.AutoFlush = true;

                if (this.Options.PrintTextOnly)
                {
                    PrintText(sw, workbookReader);
                }
                else
                {
                    PrintHtml(sw, workbookReader);
                }

                if (!_isCancelled && this.Options.ShowInBrowser && this.Options.Mode == BiffViewerMode.File)
                {
                    Util.VisitLink(this.Options.OutputFileName);
                }
            }
            catch (MagicNumberException ex)
            {
                if (this.Options.ShowErrors)
                {
                    MessageBox.Show(string.Format("This file is not a valid Excel file ({0})", ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            catch (Exception ex)
            {
                if (this.Options.ShowErrors)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
                if (sw != null)
                {
                    sw.Close();
                }
            }
        }

        protected void ShowInBrowser()
        {
            Util.VisitLink(this.Options.OutputFileName);
        }

        protected void PrintText(StreamWriter sw, IStreamReader workbookReader)
        {
            BiffHeader bh;

            try
            {
                while (workbookReader.BaseStream.Position < workbookReader.BaseStream.Length)
                {
                    bh.id = (RecordNumber)workbookReader.ReadUInt16();
                    bh.length = workbookReader.ReadUInt16();

                    byte[] buffer = new byte[bh.length];
                    if (bh.length != workbookReader.Read(buffer, bh.length))
                        sw.WriteLine("EOF");

                    sw.Write("BIFF {0}\t{1}\t", bh.id, bh.length);
                    //Dump(buffer);
                    int count = 0;
                    foreach (byte b in buffer)
                    {
                        sw.Write("{0:X02} ", b);
                        count++;
                        if (count % 16 == 0 && count < buffer.Length)
                            sw.Write("\n\t\t\t");
                    }
                    sw.Write("\n");

                    if (_backgroundWorker != null)
                    {
                        int progress = 100;

                        if (sw.BaseStream.Length != 0)
                        {
                            progress = (int)(100 * workbookReader.BaseStream.Position / workbookReader.BaseStream.Length);
                        }
                        _backgroundWorker.ReportProgress(progress);

                        if (_backgroundWorker.CancellationPending)
                        {
                            _isCancelled = true;
                            break;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        protected void PrintHtml(StreamWriter sw, IStreamReader workbookReader)
        {
            BiffHeader bh;

            Uri baseUrl = new Uri(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName);
            
            sw.WriteLine("<html>");
            sw.WriteLine("<head>");
            sw.WriteLine("<title>" + this.Options.InputDocument + "</title>");
            sw.WriteLine("<link href=\"style.css\" rel=\"stylesheet\" type=\"text/css\">");
            sw.WriteLine("<style>");
            sw.WriteLine("    td { font-family: Monospace, Courier; vertical-align: top; border-top: 1px solid black;  }");
            sw.WriteLine("    table { border: 1px solid black; empty-cells:show; border-collapse:collapse}");
            sw.WriteLine("</style>");
            sw.WriteLine("</head>");
            sw.WriteLine("<body>");
            sw.WriteLine("<table>");

            try
            {
                while (workbookReader.BaseStream.Position < workbookReader.BaseStream.Length)
                {
                    bh.id = (RecordNumber)workbookReader.ReadUInt16();
                    bh.length = workbookReader.ReadUInt16();

                    byte[] buffer = new byte[bh.length];
                    if (bh.length != workbookReader.Read(buffer, bh.length))
                        sw.WriteLine("EOF");

                    sw.WriteLine("<tr>");
                    sw.WriteLine("<td>");
                    sw.WriteLine("BIFF <a href=\"{0}\\xlsspec\\{1}.html\">{1}</a> ({2:X02}h)", baseUrl, bh.id, (int)bh.id);
                    sw.WriteLine("</td><td>");
                    sw.WriteLine("{0}", bh.length);

                    sw.WriteLine("</td><td>");


                    //Dump(buffer);
                    int count = 0;
                    foreach (byte b in buffer)
                    {
                        sw.Write("{0:X02}&nbsp;", b);
                        count++;
                        if (count % 16 == 0 && count < buffer.Length)
                            sw.Write("</br>");
                        else if (count % 8 == 0 && count < buffer.Length)
                            sw.Write("&nbsp;");
                    }
                    sw.Write("</td></tr>");

                    if (_backgroundWorker != null)
                    {
                        int progress = 100;

                        if (sw.BaseStream.Length != 0)
                        {
                            progress = (int)(100 * workbookReader.BaseStream.Position / workbookReader.BaseStream.Length);
                        }
                        _backgroundWorker.ReportProgress(progress);

                        if (_backgroundWorker.CancellationPending)
                        {
                            _isCancelled = true;
                            break;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            sw.WriteLine("</table>");
            sw.WriteLine("</body>");
            sw.WriteLine("</html>");
        }
    }
}