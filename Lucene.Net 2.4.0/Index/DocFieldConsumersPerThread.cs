/**
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace Lucene.Net.Index
{
    internal class DocFieldConsumersPerThread : DocFieldConsumerPerThread
    {
        internal readonly DocFieldConsumerPerThread one;
        internal readonly DocFieldConsumerPerThread two;
        internal readonly DocFieldConsumers parent;
        internal readonly DocumentsWriter.DocState docState;

        public DocFieldConsumersPerThread(DocFieldProcessorPerThread docFieldProcessorPerThread,
                                          DocFieldConsumers parent, DocFieldConsumerPerThread one, DocFieldConsumerPerThread two)
        {
            this.parent = parent;
            this.one = one;
            this.two = two;
            docState = docFieldProcessorPerThread.docState;
        }

        internal override void startDocument()
        {
            one.startDocument();
            two.startDocument();
        }

        internal override void abort()
        {
            try
            {
                one.abort();
            }
            finally
            {
                two.abort();
            }
        }

        internal override DocumentsWriter.DocWriter finishDocument()
        {
            DocumentsWriter.DocWriter oneDoc = one.finishDocument();
            DocumentsWriter.DocWriter twoDoc = two.finishDocument();
            if (oneDoc == null)
                return twoDoc;
            else if (twoDoc == null)
                return oneDoc;
            else
            {
                DocFieldConsumers.PerDoc both = parent.getPerDoc();
                both.docID = docState.docID;
                System.Diagnostics.Debug.Assert(oneDoc.docID == docState.docID);
                System.Diagnostics.Debug.Assert(twoDoc.docID == docState.docID);
                both.one = oneDoc;
                both.two = twoDoc;
                return both;
            }
        }

        internal override DocFieldConsumerPerField addField(FieldInfo fi)
        {
            return new DocFieldConsumersPerField(this, one.addField(fi), two.addField(fi));
        }
    }
}
