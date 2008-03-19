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
using DIaLOGIKa.b2xtranslator.CommonTranslatorLib;
using System.Xml;
using DIaLOGIKa.b2xtranslator.OpenXmlLib;
using DIaLOGIKa.b2xtranslator.DocFileFormat;

namespace DIaLOGIKa.b2xtranslator.WordprocessingMLMapping
{
    public class PropertiesMapping : AbstractOpenXmlMapping
    {
        public PropertiesMapping(XmlWriter writer)
            : base(writer)
        {
        }

        protected void appendFlagAttribute(XmlElement node, SinglePropertyModifier sprm, string attributeName)
        {
            XmlAttribute att = _nodeFactory.CreateAttribute("w", attributeName, OpenXmlNamespaces.WordprocessingML);
            att.Value = sprm.Arguments[0].ToString();
            node.Attributes.Append(att);
        }

        protected void appendFlagElement(XmlElement node, SinglePropertyModifier sprm, string elementName)
        {
            XmlElement ele = _nodeFactory.CreateElement("w", elementName, OpenXmlNamespaces.WordprocessingML);
            XmlAttribute val = _nodeFactory.CreateAttribute("w", "val", OpenXmlNamespaces.WordprocessingML);
            val.Value = sprm.Arguments[0].ToString();
            ele.Attributes.Append(val);
            node.AppendChild(ele);
        }

        protected void appendValueElement(XmlElement node, string elementName, string elementValue)
        {
            XmlElement ele = _nodeFactory.CreateElement("w", elementName, OpenXmlNamespaces.WordprocessingML);
            XmlAttribute val = _nodeFactory.CreateAttribute("w", "val", OpenXmlNamespaces.WordprocessingML);
            val.Value = elementValue;
            ele.Attributes.Append(val);
            node.AppendChild(ele);
        }

        protected void addOrSetBorder(XmlNode pBdr, XmlNode border)
        {
            //remove old border if it exist
            foreach (XmlNode bdr in pBdr.ChildNodes)
            {
                if (bdr.Name == border.Name)
                {
                    pBdr.RemoveChild(bdr);
                    break;
                }
            }

            //add new
            pBdr.AppendChild(border);
        }

        protected void appendBorderAttributes(byte[] brcBytes, XmlNode border)
        {
            //parse the border code
            BorderCode brc = new BorderCode(brcBytes);

            //create xml
            XmlAttribute val = _nodeFactory.CreateAttribute("w", "val", OpenXmlNamespaces.WordprocessingML);
            val.Value = brc.brcType.ToString();
            border.Attributes.Append(val);

            XmlAttribute color = _nodeFactory.CreateAttribute("w", "color", OpenXmlNamespaces.WordprocessingML);
            color.Value = String.Format("{0:x6}", brc.cv);
            border.Attributes.Append(color);

            XmlAttribute space = _nodeFactory.CreateAttribute("w", "space", OpenXmlNamespaces.WordprocessingML);
            space.Value = brc.dptSpace.ToString();
            border.Attributes.Append(space);

            XmlAttribute sz = _nodeFactory.CreateAttribute("w", "sz", OpenXmlNamespaces.WordprocessingML);
            sz.Value = brc.dptLineWidth.ToString();
            border.Attributes.Append(sz);
        }
    }
}
