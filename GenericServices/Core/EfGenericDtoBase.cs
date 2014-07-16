﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using GenericServices.Core.Internal;

[assembly: InternalsVisibleTo("Tests")]

namespace GenericServices.Core
{
    [Flags]
    public enum ServiceFunctions
    {
        None = 0,
        List = 1,
        Detail = 2,
        Create = 4,
        Update = 8,
        //note: no delete as delete does not need a dto
        
        //Now Action parts
        DoActionWithoutValidate = 32,
        //This is the default. It validates the destination before calling the action
        DoAction = DoActionWithoutValidate | ValidateonCopyDtoToData,
        //This causes the destination data is validated after a CopyDtoToData. 
        //(Not really necessary when doing a DB action as SaveChanges does a validation)
        ValidateonCopyDtoToData = 64,
        //DoesNotNeedSetup refers the need to call the SetupSecondaryData method
        //if this flag is NOT set then expects dto to override SetupSecondaryData method
        DoesNotNeedSetup = 256,
        AllCrudButCreate = List | Detail | Update,
        AllCrudButList = Detail | Create | Update,
        AllCrud = List | Detail | Create | Update
    }

    public abstract class EfGenericDtoBase<TData, TDto> 
        where TData : class
        where TDto : EfGenericDtoBase<TData, TDto>
    {
        /// <summary>
        /// Optional method that will setup any mapping etc. that are cached. This will will improve speed later.
        /// The GenericDto will still work without this method being called, but the first use that needs the map will be slower. 
        /// </summary>
        public void CacheSetup()
        {
            CreateDatatoDtoMapping();
            CreateDtoToDataMapping();
        }

        /// <summary>
        /// This must be overridden to say that the dto supports the create function
        /// </summary>
        internal protected abstract ServiceFunctions SupportedFunctions { get; }

        /// <summary>
        /// This provides the name of the name of the data item to display in success or error messages.
        /// Override if you want a more user friendly name
        /// </summary>
        internal protected virtual string DataItemName { get { return typeof (TData).Name; }}
        
        /// <summary>
        /// This method is called to get the data table. Can be overridden if include statements are needed.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>returns an IQueryable of the table TData as Untracked</returns>
        protected virtual IQueryable<TData> GetDataUntracked(IDbContextWithValidation context)
        {
            return context.Set<TData>().AsNoTracking();
        }

        /// <summary>
        /// This provides the IQueryable command to get a list of TData, but projected to TDto.
        /// Can be overridden if standard AutoMapping isn't good enough, or return null if not supported
        /// </summary>
        /// <returns></returns>
        internal protected virtual IQueryable<TDto> BuildListQueryUntracked(IDbContextWithValidation context)
        {
            CreateDatatoDtoMapping();
            return GetDataUntracked(context).Project().To<TDto>();
        }

        //---------------------------------------------------------------
        //protected methods

        protected object[] GetKeyValues(IDbContextWithValidation context)
        {
            var efkeyPropertyNames = context.GetKeyProperties<TData>().Select(x => x.Name).ToArray();

            var dtoKeyProperies = typeof(TDto).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(x => efkeyPropertyNames.Any( y => y == x.Name)).ToArray();

            if (efkeyPropertyNames.Length != dtoKeyProperies.Length)
                throw new MissingPrimaryKeyException("The dto did not ");

            return dtoKeyProperies.Select(x => x.GetValue(this)).ToArray();
        }

        protected static void CreateDatatoDtoMapping()
        {
            Mapper.CreateMap<TData, TDto>();
        }

        protected static void CreateDtoToDataMapping()
        {
            Mapper.CreateMap<TDto, TData>()
                .ForAllMembers(opt => opt.Condition(CheckIfSourceSetterIsPublic));
        }

        //----------------------------------------------------------------
        //private methods

        private static bool CheckIfSourceSetterIsPublic(ResolutionContext mapContext)
        {
            return mapContext.PropertyMap.SourceMember != null 
                   && ((PropertyInfo)mapContext.PropertyMap.SourceMember).SetMethod != null
                   && ((PropertyInfo)mapContext.PropertyMap.SourceMember).SetMethod.IsPublic;
        }

    }
}
