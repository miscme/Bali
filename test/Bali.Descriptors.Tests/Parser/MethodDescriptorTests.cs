using System;
using Xunit;

namespace Bali.Descriptors.Tests.Parser
{
    public class MethodDescriptorTests
    {
        [Fact]
        public void MissingParameters()
        {
            var lexer = new DescriptorLexer("V".AsMemory());
            var parser = new MethodDescriptorParser(lexer.Lex());

            Assert.Throws<DescriptorParserException>(() => parser.Parse());
        }

        [Fact]
        public void MissingReturnType()
        {
            var lexer = new DescriptorLexer("()".AsMemory());
            var parser = new MethodDescriptorParser(lexer.Lex());

            Assert.Throws<DescriptorParserException>(() => parser.Parse());
        }

        [Theory]
        [InlineData("(IDFJ)Z", new [] { PrimitiveKind.Int, PrimitiveKind.Double, PrimitiveKind.Float, PrimitiveKind.Long }, PrimitiveKind.Boolean)]
        [InlineData("(SZBC)V", new [] { PrimitiveKind.Short, PrimitiveKind.Boolean, PrimitiveKind.Byte, PrimitiveKind.Char }, PrimitiveKind.Void)]
        public void AllPrimitive(string input, PrimitiveKind[] parameters, PrimitiveKind returnType)
        {
            var lexer = new DescriptorLexer(input.AsMemory());
            var parser = new MethodDescriptorParser(lexer.Lex());
            var descriptor = parser.Parse();

            for (int i = 0; i < descriptor.Parameters.Count; i++)
                Assert.Equal(parameters[i], Assert.IsType<PrimitiveFieldDescriptor>(descriptor.Parameters[i]).Kind);
            
            Assert.Equal(returnType, Assert.IsType<PrimitiveFieldDescriptor>(descriptor.ReturnType).Kind);
        }

        [Theory]
        [InlineData("([[I[D[[[J)[Z", new [] { PrimitiveKind.Int, PrimitiveKind.Double, PrimitiveKind.Long }, new [] { 2, 1, 3 }, PrimitiveKind.Boolean, 1)]
        [InlineData("(S[[Z[[[[B)[[C", new [] { PrimitiveKind.Short, PrimitiveKind.Boolean, PrimitiveKind.Byte }, new [] { 0, 2, 4 }, PrimitiveKind.Char, 2)]
        public void ArrayAllPrimitive(string input, PrimitiveKind[] parameterTypes, int[] parameterArrayRanks, PrimitiveKind returnType, int returnArrayRank)
        {
            var lexer = new DescriptorLexer(input.AsMemory());
            var parser = new MethodDescriptorParser(lexer.Lex());
            var descriptor = parser.Parse();

            for (int i = 0; i < descriptor.Parameters.Count; i++)
            {
                Assert.Equal(parameterTypes[i], Assert.IsType<PrimitiveFieldDescriptor>(descriptor.Parameters[i]).Kind);
                Assert.Equal(parameterArrayRanks[i], descriptor.Parameters[i].ArrayRank);
            }
            
            Assert.Equal(returnType, Assert.IsType<PrimitiveFieldDescriptor>(descriptor.ReturnType).Kind);
            Assert.Equal(returnArrayRank, descriptor.ReturnType.ArrayRank);
        }

        [Theory]
        [InlineData("(Ljava/lang/Object;Ljava/util/Scanner;)Ljava/lang/String;", new [] { "java/lang/Object", "java/util/Scanner" }, "java/lang/String")]
        [InlineData("(Ljava/util/List;Ljava/lang/Object;)Ljava/lang/Integer;", new [] { "java/util/List", "java/lang/Object" }, "java/lang/Integer")]
        public void AllNonPrimitive(string input, string[] parameters, string returnType)
        {
            var lexer = new DescriptorLexer(input.AsMemory());
            var parser = new MethodDescriptorParser(lexer.Lex());
            var descriptor = parser.Parse();

            for (int i = 0; i < descriptor.Parameters.Count; i++)
                Assert.Equal(parameters[i], Assert.IsType<NonPrimitiveFieldDescriptor>(descriptor.Parameters[i]).ClassName);
            
            Assert.Equal(returnType, Assert.IsType<NonPrimitiveFieldDescriptor>(descriptor.ReturnType).ClassName);
        }

        [Theory]
        [InlineData("([[[Ljava/lang/Long;[[Ljava/lang/Object;)[[Ljava/lang/Boolean;", new [] { "java/lang/Long", "java/lang/Object" }, new [] { 3, 2 }, "java/lang/Boolean", 2)]
        [InlineData("([[[[[Ljava/util/List;[[[Ljava/lang/Integer;)[[[[Ljava/lang/Object;", new [] { "java/util/List", "java/lang/Integer" }, new [] { 5, 3 }, "java/lang/Object", 4)]
        public void ArrayAllNonPrimitive(string input, string[] parameters, int[] parameterRanks, string returnType, int returnRank)
        {
            var lexer = new DescriptorLexer(input.AsMemory());
            var parser = new MethodDescriptorParser(lexer.Lex());
            var descriptor = parser.Parse();

            for (int i = 0; i < descriptor.Parameters.Count; i++)
            {
                Assert.Equal(parameterRanks[i], descriptor.Parameters[i].ArrayRank);
                Assert.Equal(parameters[i], Assert.IsType<NonPrimitiveFieldDescriptor>(descriptor.Parameters[i]).ClassName);
            }
            
            Assert.Equal(returnRank, descriptor.ReturnType.ArrayRank);
            Assert.Equal(returnType, Assert.IsType<NonPrimitiveFieldDescriptor>(descriptor.ReturnType).ClassName);
        }

        [Fact]
        public void Generic()
        {
            var lexer = new DescriptorLexer("(Ljava/util/Pair<Lcom/company/CustomStuff;Ljava/lang/String;>;)Ljava/util/HashMap<Ljava/lang/Integer;Ljava/lang/String;>;".AsMemory());
            var parser = new MethodDescriptorParser(lexer.Lex());
            var descriptor = parser.Parse();
            
            Assert.Equal(1, descriptor.Parameters.Count);
            Assert.IsType<NonPrimitiveFieldDescriptor>(descriptor.Parameters[0]);
            var pair = (NonPrimitiveFieldDescriptor) descriptor.Parameters[0];
            Assert.Equal("java/util/Pair", pair.ClassName);
            Assert.Equal(2, pair.GenericParameters.Count);
            Assert.Equal("com/company/CustomStuff", Assert.IsType<NonPrimitiveFieldDescriptor>(pair.GenericParameters[0]).ClassName);
            Assert.Equal("java/lang/String", Assert.IsType<NonPrimitiveFieldDescriptor>(pair.GenericParameters[1]).ClassName);
            Assert.IsType<NonPrimitiveFieldDescriptor>(descriptor.ReturnType);
            var returnType = (NonPrimitiveFieldDescriptor) descriptor.ReturnType;
            Assert.Equal("java/util/HashMap", returnType.ClassName);
            Assert.Equal(2, returnType.GenericParameters.Count);
            Assert.Equal("java/lang/Integer", Assert.IsType<NonPrimitiveFieldDescriptor>(returnType.GenericParameters[0]).ClassName);
            Assert.Equal("java/lang/String", Assert.IsType<NonPrimitiveFieldDescriptor>(returnType.GenericParameters[1]).ClassName);
        }

        [Fact]
        public void ArrayGeneric()
        {
            var lexer = new DescriptorLexer("([[Ljava/util/Pair<[Lcom/company/CustomStuff;[[[Ljava/lang/String;>;)[[[[Ljava/util/HashMap<Ljava/lang/Integer;Ljava/lang/String;>;".AsMemory());
            var parser = new MethodDescriptorParser(lexer.Lex());
            var descriptor = parser.Parse();
            
            Assert.Equal(1, descriptor.Parameters.Count);
            Assert.IsType<NonPrimitiveFieldDescriptor>(descriptor.Parameters[0]);
            Assert.Equal(2, descriptor.Parameters[0].ArrayRank);
            var pair = (NonPrimitiveFieldDescriptor) descriptor.Parameters[0];
            Assert.Equal("java/util/Pair", pair.ClassName);
            Assert.Equal(2, pair.GenericParameters.Count);
            Assert.Equal(1, pair.GenericParameters[0].ArrayRank);
            Assert.Equal("com/company/CustomStuff", Assert.IsType<NonPrimitiveFieldDescriptor>(pair.GenericParameters[0]).ClassName);
            Assert.Equal(3, pair.GenericParameters[1].ArrayRank);
            Assert.Equal("java/lang/String", Assert.IsType<NonPrimitiveFieldDescriptor>(pair.GenericParameters[1]).ClassName);
            Assert.IsType<NonPrimitiveFieldDescriptor>(descriptor.ReturnType);
            var returnType = (NonPrimitiveFieldDescriptor) descriptor.ReturnType;
            Assert.Equal(4, returnType.ArrayRank);
            Assert.Equal("java/util/HashMap", returnType.ClassName);
            Assert.Equal(2, returnType.GenericParameters.Count);
            Assert.Equal("java/lang/Integer", Assert.IsType<NonPrimitiveFieldDescriptor>(returnType.GenericParameters[0]).ClassName);
            Assert.Equal("java/lang/String", Assert.IsType<NonPrimitiveFieldDescriptor>(returnType.GenericParameters[1]).ClassName);
        }
    }
}