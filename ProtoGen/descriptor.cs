// Protocol Buffers - Google's data interchange format
// Copyright 2008 Google Inc.  All rights reserved.
// http://code.google.com/p/protobuf/
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above
// copyright notice, this list of conditions and the following disclaimer
// in the documentation and/or other materials provided with the
// distribution.
//     * Neither the name of Google Inc. nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

// Author: kenton@google.com (Kenton Varda)
//  Based on original Protocol Buffers design by
//  Sanjay Ghemawat, Jeff Dean, and others.
//
// The messages in this file describe the definitions found in .proto files.
// A valid .proto file can be translated directly to a FileDescriptorProto
// without any other information (e.g. without reading its imports).




using System.ComponentModel;
using ProtoBuf;
using System.Collections.Generic;
using System;
namespace google.protobuf {

[Serializable, ProtoContract(Name=@"FileDescriptorSet")]
public sealed class FileDescriptorSet {
  
    [ProtoMember(1, Name=@"file")]
    public List<FileDescriptorProto> file {get; set;}

}

// Describes a complete .proto file.

[Serializable, ProtoContract(Name=@"FileDescriptorProto")]
public sealed class FileDescriptorProto {
  
    [ProtoMember(1, Name=@"name")]
    public string name {get; set;}
       // file name, relative to root of source tree
  
    [ProtoMember(2, Name=@"package")]
    public string package {get; set;}
    // e.g. "foo", "foo.bar", etc.

  // Names of files imported by this file.
  
    [ProtoMember(3, Name=@"dependency")]
    public List<string> dependency {get; set;}


  // All top-level definitions in this file.
  
    [ProtoMember(4, Name=@"message_type")]
    public List<DescriptorProto> message_type {get; set;}

  
    [ProtoMember(5, Name=@"enum_type")]
    public List<enumDescriptorProto> enum_type {get; set;}

  
    [ProtoMember(6, Name=@"service")]
    public List<ServiceDescriptorProto> service {get; set;}

  
    [ProtoMember(7, Name=@"extension")]
    public List<FieldDescriptorProto> extension {get; set;}


  
    [ProtoMember(8, Name=@"options")]
    public FileOptions options {get; set;}

}

// Describes a message type.

[Serializable, ProtoContract(Name=@"DescriptorProto")]
public sealed class DescriptorProto {
  
    [ProtoMember(1, Name=@"name")]
    public string name {get; set;}


  
    [ProtoMember(2, Name=@"field")]
    public List<FieldDescriptorProto> field {get; set;}

  
    [ProtoMember(6, Name=@"extension")]
    public List<FieldDescriptorProto> extension {get; set;}


  
    [ProtoMember(3, Name=@"nested_type")]
    public List<DescriptorProto> nested_type {get; set;}

  
    [ProtoMember(4, Name=@"enum_type")]
    public List<enumDescriptorProto> enum_type {get; set;}


  
[Serializable, ProtoContract(Name=@"ExtensionRange")]
public sealed class ExtensionRange {
    
    [ProtoMember(1, Name=@"start")]
    public int start {get; set;}

    
    [ProtoMember(2, Name=@"end")]
    public int end {get; set;}

  }
  
    [ProtoMember(5, Name=@"extension_range")]
    public List<ExtensionRange> extension_range {get; set;}


  
    [ProtoMember(7, Name=@"options")]
    public MessageOptions options {get; set;}

}

// Describes a field within a message.

[Serializable, ProtoContract(Name=@"FieldDescriptorProto")]
public sealed class FieldDescriptorProto {
  public enum Type {
    // 0 is reserved for errors.
    // Order is weird for historical reasons.
    TYPE_DOUBLE         = 1,
    TYPE_FLOAT          = 2,
    TYPE_long          = 3,   // Not ZigZag encoded.  Negative numbers
                               // take 10 bytes.  Use TYPE_Slong if negative
                               // values are likely.
    TYPE_ulong         = 4,
    TYPE_INT32          = 5,   // Not ZigZag encoded.  Negative numbers
                               // take 10 bytes.  Use TYPE_SINT32 if negative
                               // values are likely.
    TYPE_FIXED64        = 6,
    TYPE_FIXED32        = 7,
    TYPE_BOOL           = 8,
    TYPE_STRING         = 9,
    TYPE_GROUP          = 10,  // Tag-delimited aggregate.
    TYPE_MESSAGE        = 11,  // Length-delimited aggregate.

    // New in version 2.
    TYPE_BYTES          = 12,
    TYPE_UINT32         = 13,
    TYPE_ENUM           = 14,
    TYPE_SFIXED32       = 15,
    TYPE_SFIXED64       = 16,
    TYPE_SINT32         = 17,  // Uses ZigZag encoding.
    TYPE_Slong         = 18  // Uses ZigZag encoding.
  };

  public enum Label {
    // 0 is reserved for errors
    LABEL_OPTIONAL      = 1,
    LABEL_REQUIRED      = 2,
    LABEL_REPEATED      = 3
    // TODO(sanjay): Should we add LABEL_MAP
  };

  
    [ProtoMember(1, Name=@"name")]
    public string name {get; set;}

  
    [ProtoMember(3, Name=@"number")]
    public int number {get; set;}

  
    [ProtoMember(4, Name=@"label")]
    [DefaultValue(Label.LABEL_OPTIONAL)]
    public Label label {get; set;}


  // If type_name is set, this need not be set.  If both this and type_name
  // are set, this must be either TYPE_public enum or TYPE_MESSAGE.
  
    [ProtoMember(5, Name=@"type")]
    [DefaultValue(Type.TYPE_INT32)]
    public Type type {get; set;}


  // For message and public enum types, this is the name of the type.  If the name
  // starts with a '.', it is fully-qualified.  Otherwise, C++-like scoping
  // rules are used to find the type (i.e. first the nested types within this
  // message are searched, then within the parent, on up to the root
  // namespace).
  
    [ProtoMember(6, Name=@"type_name")]
    public string type_name {get; set;}


  // For extensions, this is the name of the type being extended.  It is
  // resolved in the same manner as type_name.
  
    [ProtoMember(2, Name=@"extendee")]
    public string extendee {get; set;}


  // For numeric types, contains the original text representation of the value.
  // For booleans, "true" or "false".
  // For strings, contains the default text contents (not escaped in any way).
  // For bytes, contains the C escaped value.  All bytes >= 128 are escaped.
  // TODO(kenton):  Base-64 encode
  
    [ProtoMember(7, Name=@"default_value")]
    public string default_value {get; set;}


  
    [ProtoMember(8, Name=@"options")]
    public MessageOptions.FieldOptions options {get; set;}

}

// Describes an public enum type.

[Serializable, ProtoContract(Name=@"public enumDescriptorProto")]
public sealed class enumDescriptorProto {
  
    [ProtoMember(1, Name=@"name")]
    public string name {get; set;}


  
    [ProtoMember(2, Name=@"value")]
    public List<enumValueDescriptorProto> value {get; set;}


  
    [ProtoMember(3, Name=@"options")]
    public MessageOptions.FieldOptions.Item.enumOptions options {get; set;}

}

// Describes a value within an public enum.

[Serializable, ProtoContract(Name=@"public enumValueDescriptorProto")]
public sealed class enumValueDescriptorProto {
  
    [ProtoMember(1, Name=@"name")]
    public string name {get; set;}

  
    [ProtoMember(2, Name=@"number")]
    public int number {get; set;}


  
    [ProtoMember(3, Name=@"options")]
    public MessageOptions.FieldOptions.Item.enumValueOptions options {get; set;}

}

// Describes a service.

[Serializable, ProtoContract(Name=@"ServiceDescriptorProto")]
public sealed class ServiceDescriptorProto {
  
    [ProtoMember(1, Name=@"name")]
    public string name {get; set;}

  
    [ProtoMember(2, Name=@"method")]
    public List<MethodDescriptorProto> method {get; set;}


  
    [ProtoMember(3, Name=@"options")]
    public MessageOptions.FieldOptions.Item.ServiceOptions options {get; set;}

}

// Describes a method of a service.

[Serializable, ProtoContract(Name=@"MethodDescriptorProto")]
public sealed class MethodDescriptorProto {
  
    [ProtoMember(1, Name=@"name")]
    public string name {get; set;}


  // Input and output type names.  These are resolved in the same way as
  // FieldDescriptorProto.type_name, but must refer to a message type.
  
    [ProtoMember(2, Name=@"input_type")]
    public string input_type {get; set;}

  
    [ProtoMember(3, Name=@"output_type")]
    public string output_type {get; set;}


  
    [ProtoMember(4, Name=@"options")]
    public MessageOptions.FieldOptions.Item.MethodOptions options {get; set;}

}

// ===================================================================
// Options

// Each of the definitions above may have "options" attached.  These are
// just annotations which may cause code to be generated slightly differently
// or may contain hints for code that manipulates protocol messages.
//
// Clients may define custom options as extensions of the *Options messages.
// These extensions may not yet be known at parsing time, so the parser cannot
// store the values in them.  Instead it stores them in a field in the *Options
// message called uninterpreted_option. This field must have the same name
// across all *Options messages. We then use this field to populate the
// extensions when we build a descriptor, at which point all protos have been
// parsed and so all extensions are known.
//
// Extension numbers for custom options may be chosen as follows:
// * For options which will only be used within a single application or
//   organization, or for experimental options, use field numbers 50000
//   through 99999.  It is up to you to ensure that you do not use the
//   same number for multiple options.
// * For options which will be published and used publicly by multiple
//   independent entities, e-mail kenton@google.com to reserve extension
//   numbers.  Simply tell me how many you need and I'll send you back a
//   set of numbers to use -- there's no need to explain how you intend to
//   use them.  If this turns out to be popular, a web service will be set up
//   to automatically assign option numbers.



[Serializable, ProtoContract(Name=@"FileOptions")]
public sealed class FileOptions {

  // Sets the Java package where classes generated from this .proto will be
  // placed.  By default, the proto package is used, but this is often
  // inappropriate because proto packages do not normally start with backwards
  // domain names.
  
    [ProtoMember(1, Name=@"java_package")]
    public string java_package {get; set;}



  // If set, all the classes from the .proto file are wrapped in a single
  // outer class with the given name.  This applies to both Proto1
  // (equivalent to the old "--one_java_file" option) and Proto2 (where
  // a .proto always translates to a single class, but you may want to
  // explicitly choose the class name).
  
    [ProtoMember(8, Name=@"java_outer_classname")]
    public string java_outer_classname {get; set;}


  // If set true, then the Java code generator will generate a separate .java
  // file for each top-level message, public enum, and service defined in the .proto
  // file.  Thus, these types will *not* be nested inside the outer class
  // named by java_outer_classname.  However, the outer class will still be
  // generated to contain the file's getDescriptor() method as well as any
  // top-level extensions defined in the file.

  [DefaultValue(false)]
  [ProtoMember(10, Name="java_multiple_files")]
  public bool java_multiple_files { get; set;}

  // Generated classes can be optimized for speed or code size.
  public enum OptimizeMode {
    SPEED = 1,      // Generate complete code for parsing, serialization, etc.
    CODE_SIZE = 2  // Use ReflectionOps to implement these methods.
  }
    public FileOptions()
    {
        optimize_for = OptimizeMode.CODE_SIZE;
    }
    [ProtoMember(9, Name="optimize_for")]
    [DefaultValue(OptimizeMode.CODE_SIZE)]
    public OptimizeMode optimize_for { get; set;}
}


}


[Serializable, ProtoContract(Name = @"MessageOptions")]
public sealed class MessageOptions
{
    // Set true to use the old proto1 MessageSet wire format for extensions.
    // This is provided for backwards-compatibility with the MessageSet wire
    // format.  You should not use this for any other reason:  It's less
    // efficient, has fewer features, and is more complicated.
    //
    // The message must be defined exactly as follows:
    //   
    [Serializable, ProtoContract(Name = @"Foo")]
    public sealed class Foo
    {
        //     option message_set_wire_format = true;
        //     extensions 4 to max;
        //   }
        // Note that the message cannot have any defined fields; MessageSets only
        // have extensions.
        //
        // All extensions of your type must be singular messages; e.g. they cannot
        // be int32s, public enums, or repeated messages.
        //
        // Because this is an option, the above two restrictions are not enforced by
        // the protocol compiler.

        [ProtoMember(1)]
        public bool message_set_wire_format { get; set; }


    }


    [Serializable, ProtoContract(Name = @"FieldOptions")]
    public sealed class FieldOptions
    {
        // The ctype option instructs the C++ code generator to use a different
        // representation of the field than it normally would.  See the specific
        // options below.  This option is not yet implemented in the open source
        // release -- sorry, we'll try to include it in a future version!

        [ProtoMember(1, Name = @"ctype")]
        [DefaultValue(CType.CORD)]
        public CType ctype { get; set; }

        public enum CType
        {
            CORD = 1,
            STRING_PIECE = 2
        }

        // EXPERIMENTAL.  DO NOT USE.
        // For "map" fields, the name of the field in the enclosed type that
        // is the key for this map.  For example, suppose we have:
        //   
        [Serializable, ProtoContract(Name = @"Item")]
        public sealed class Item
        {
            //     required string name = 1;
            //     required string value = 2;
            //   }
            //   
            [Serializable, ProtoContract(Name = @"Config")]
            public sealed class Config
            {
                //     repeated Item items = 1 [experimental_map_key="name"];
                //   }
                // In this situation, the map key for Item will be set to "name".
                // TODO: Fully-implement this, then remove the "experimental_" prefix.

                [ProtoMember(9, Name = @"experimental_map_key")]
                public string experimental_map_key { get; set; }


            }


            [Serializable, ProtoContract(Name = @"public enumOptions")]
            public sealed class enumOptions
            {
                // The parser stores options it doesn't recognize here. See above.


            }


            [Serializable, ProtoContract(Name = @"public enumValueOptions")]
            public sealed class enumValueOptions
            {

            }


            [Serializable, ProtoContract(Name = @"ServiceOptions")]
            public sealed class ServiceOptions
            {

                // Note:  Field numbers 1 through 32 are reserved for Google's internal RPC
                //   framework.  We apologize for hoarding these numbers to ourselves, but
                //   we were already using them long before we decided to release Protocol
                //   Buffers.


            }


            [Serializable, ProtoContract(Name = @"MethodOptions")]
            public sealed class MethodOptions
            {

                // Note:  Field numbers 1 through 32 are reserved for Google's internal RPC
                //   framework.  We apologize for hoarding these numbers to ourselves, but
                //   we were already using them long before we decided to release Protocol
                //   Buffers.


            }

            // A message representing a option the parser does not recognize. This only
            // appears in options protos created by the compiler::Parser class.
            // DescriptorPool resolves these when building Descriptor objects. Therefore,
            // options protos in descriptor objects (e.g. returned by Descriptor::options(),
            // or produced by Descriptor::CopyTo()) will never have UninterpretedOptions
            // in them.

            [Serializable, ProtoContract(Name = @"UninterpretedOption")]
            public sealed class UninterpretedOption
            {
                // The name of the uninterpreted option.  Each string represents a segment in
                // a dot-separated name.  is_extension is true iff a segment represents an
                // extension (denoted with parentheses in options specs in .proto files).
                // E.g.,{ ["foo", false], ["bar.baz", true], ["qux", false] } represents
                // "foo.(bar.baz).qux".

                [Serializable, ProtoContract(Name = @"NamePart")]
                public sealed class NamePart
                {
                    [ProtoMember(1)]
                    public string name_part { get; set; }
                    [ProtoMember(2)]
                    public bool is_extension { get; set; }
                }

                [ProtoMember(2, Name = @"name")]
                public List<NamePart> name { get; private set; }


                // The value of the uninterpreted option, in whatever type the tokenizer
                // identified it as during parsing. Exactly one of these should be set.

                [ProtoMember(3, Name = @"identifier_value")]
                public string identifier_value { get; set; }


                [ProtoMember(4, Name = @"positive_int_value")]
                public ulong positive_int_value { get; set; }


                [ProtoMember(5, Name = @"negative_int_value")]
                public long negative_int_value { get; set; }


                [ProtoMember(6, Name = @"double_value")]
                public double double_value { get; set; }


                [ProtoMember(7, Name = @"string_value")]
                public byte[] string_value { get; set; }

            }
        }
    }
}