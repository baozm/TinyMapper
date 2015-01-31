﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TinyMappers.CodeGenerators;
using TinyMappers.CodeGenerators.Emitters;
using TinyMappers.Core;
using TinyMappers.DataStructures;
using TinyMappers.Reflection;

namespace TinyMappers.Mappers.Collections
{
    internal sealed class CollectionMapperBuilder
    {
        private const BindingFlags InstanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string MapperNamePrefix = "TinyCollection";
        private const MethodAttributes OverrideProtected = MethodAttributes.Family | MethodAttributes.Virtual;

        public static Mapper Create(IDynamicAssembly assembly, TypePair typePair)
        {
            Type parentType = typeof(CollectionMapper<,>).MakeGenericType(typePair.Source, typePair.Target);
            TypeBuilder typeBuilder = assembly.DefineType(GetMapperName(), parentType);
            if (IsList(typePair.Target))
            {
                EmitToList(parentType, typeBuilder, typePair);
            }

            var result = (Mapper)Activator.CreateInstance(typeBuilder.CreateType());
            return result;
        }

        private static void EmitToList(Type parentType, TypeBuilder typeBuilder, TypePair typePair)
        {
            MethodBuilder methodBuilder = typeBuilder.DefineMethod("EnumerableToList", OverrideProtected, typePair.Target, new[] { Types.IEnumerable });

            Type targetItemType = GetCollectionItemType(typePair.Target);

            MethodInfo methodTemplate = parentType
                .GetMethod("EnumerableToListTemplate", InstanceNonPublic)
                .MakeGenericMethod(targetItemType);

            IEmitterType returnValue = EmitterMethod.Call(methodTemplate, EmitterThis.Load(parentType), EmitterArgument.Load(Types.Object, 1));
            EmitterReturn.Return(returnValue).Emit(new CodeGenerator(methodBuilder.GetILGenerator()));
        }

        private static Type GetCollectionItemType(Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }
            else if (IsList(type))
            {
                return type.GetGenericArguments().First();
            }
            throw new NotSupportedException();
        }

        private static string GetMapperName()
        {
            string random = Guid.NewGuid().ToString("N");
            return string.Format("{0}_{1}", MapperNamePrefix, random);
        }

        private static bool IsList(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }
    }


    internal abstract class CollectionMapper<TSource, TTarget> : MapperOf<TSource, TTarget>
    {
        internal override TTarget MapCore(TSource source, TTarget target)
        {
            return EnumerableToList((IEnumerable)source);
        }

//        internal override object MapCore(object source, object target)
//        {
//            return EnumerableToList((IEnumerable)source);
//        }

        protected virtual TTarget EnumerableToList(IEnumerable value)
        {
            throw new NotImplementedException();
        }

        protected List<TItem> EnumerableToListTemplate<TItem>(IEnumerable source)
        {
            var result = new List<TItem>();
            foreach (object item in source)
            {
                result.Add((TItem)item);
            }
            return result;
        }
    }
}
