//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.SCIM
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    public abstract class Resource : Schematized
    {
        [DataMember(Name = AttributeNames.ExternalIdentifier, IsRequired = false, EmitDefaultValue = false)]
        public string? ExternalIdentifier
        {
            get;
            set;
        }

        [DataMember(Name = AttributeNames.Identifier)]
        public string? Identifier
        {
            get;
            set;
        }

        public virtual bool TryGetIdentifier(Uri baseIdentifier, out Uri identifier)
        {
            identifier = new Uri("http://unknown");
            return false;
        }

        public virtual bool TryGetPathIdentifier(out Uri pathIdentifier)
        {
            pathIdentifier = new Uri("http://unknown");
            return false;
        }
    }
}