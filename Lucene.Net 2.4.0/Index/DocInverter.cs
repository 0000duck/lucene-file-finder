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

using System.Collections.Generic;

namespace Lucene.Net.Index
{
    /// <summary>
    /// This is a DocFieldConsumer that inverts each field,
    /// separately, from a Document, and accepts a
    /// InvertedTermsConsumer to process those terms.
    /// </summary>
    internal sealed class DocInverter : DocFieldConsumer
    {
        internal readonly InvertedDocConsumer consumer;
        internal readonly InvertedDocEndConsumer endConsumer;

        public DocInverter(InvertedDocConsumer consumer, InvertedDocEndConsumer endConsumer)
        {
            this.consumer = consumer;
            this.endConsumer = endConsumer;
        }

        internal override void setFieldInfos(FieldInfos fieldInfos)
        {
            base.setFieldInfos(fieldInfos);
            consumer.setFieldInfos(fieldInfos);
            endConsumer.setFieldInfos(fieldInfos);
        }

        //internal override void flush(IDictionary<DocFieldConsumerPerThread, ICollection<DocFieldConsumerPerField>> threadsAndFields, DocumentsWriter.FlushState state)
        internal override void flush(IDictionary<object, ICollection<object>> threadsAndFields, DocumentsWriter.FlushState state)
        {
            //IDictionary<DocFieldConsumerPerThread, ICollection<DocFieldConsumerPerField>> childThreadsAndFields = new Dictionary<DocFieldConsumerPerThread, ICollection<DocFieldConsumerPerField>>();
            //IDictionary<DocFieldConsumerPerThread, ICollection<DocFieldConsumerPerField>> endChildThreadsAndFields = new Dictionary<DocFieldConsumerPerThread, ICollection<DocFieldConsumerPerField>>();
            IDictionary<object, ICollection<object>> childThreadsAndFields = new Dictionary<object, ICollection<object>>();
            IDictionary<object, ICollection<object>> endChildThreadsAndFields = new Dictionary<object, ICollection<object>>();

            //IEnumerator<KeyValuePair<DocFieldConsumerPerThread, ICollection<DocFieldConsumerPerField>>> it = threadsAndFields.GetEnumerator();
            IEnumerator<KeyValuePair<object, ICollection<object>>> it = threadsAndFields.GetEnumerator();
            while (it.MoveNext())
            {
                //KeyValuePair<DocFieldConsumerPerThread, ICollection<DocFieldConsumerPerField>> entry = it.Current;
                KeyValuePair<object, ICollection<object>> entry = it.Current;

                DocInverterPerThread perThread = (DocInverterPerThread)entry.Key;

                //ICollection<DocFieldConsumerPerField> fields = entry.Value;
                ICollection<object> fields = entry.Value;
                //IEnumerator<DocFieldConsumerPerField> fieldsIt = fields.GetEnumerator();
                IEnumerator<object> fieldsIt = fields.GetEnumerator();
                
                //IDictionary<DocFieldConsumerPerField, DocFieldConsumerPerField> childFields = new Dictionary<DocFieldConsumerPerField, DocFieldConsumerPerField>();
                //IDictionary<DocFieldConsumerPerField, DocFieldConsumerPerField> endChildFields = new Dictionary<DocFieldConsumerPerField, DocFieldConsumerPerField>();
                IDictionary<object, object> childFields = new Dictionary<object, object>();
                IDictionary<object, object> endChildFields = new Dictionary<object, object>();
 
                while (fieldsIt.MoveNext())
                {
                    DocInverterPerField perField = (DocInverterPerField)fieldsIt.Current;
                    childFields[perField.consumer] = perField.consumer;
                    endChildFields[perField.endConsumer] = perField.endConsumer;
                }

                childThreadsAndFields[perThread.consumer] = childFields.Keys;
                // create new collection to provide for deletions in NormsWriter
                endChildThreadsAndFields[perThread.endConsumer] = new List<object>(endChildFields.Keys);
            }

            consumer.flush(childThreadsAndFields, state);
            endConsumer.flush(endChildThreadsAndFields, state);
        }

        internal override void closeDocStore(DocumentsWriter.FlushState state)
        {
            consumer.closeDocStore(state);
            endConsumer.closeDocStore(state);
        }

        internal override void Abort()
        {
            consumer.abort();
            endConsumer.abort();
        }

        internal override bool freeRAM()
        {
            return consumer.freeRAM();
        }

        internal override DocFieldConsumerPerThread addThread(DocFieldProcessorPerThread docFieldProcessorPerThread)
        {
            return new DocInverterPerThread(docFieldProcessorPerThread, this);
        }

        internal sealed class FieldInvertState
        {
            internal int position;
            internal int length;
            internal int offset;
            internal float boost;

            internal void reset(float docBoost)
            {
                position = 0;
                length = 0;
                offset = 0;
                boost = docBoost;
            }
        }
    }
}
