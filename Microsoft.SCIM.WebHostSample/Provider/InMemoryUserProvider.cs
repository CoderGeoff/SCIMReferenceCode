// Copyright (c) Microsoft Corporation.// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;

namespace Microsoft.SCIM.WebHostSample.Provider
{
    public class InMemoryUserProvider : ProviderBase
    {
        private readonly InMemoryStorage storage;

        public InMemoryUserProvider()
        {
            storage = InMemoryStorage.Instance;
        }

        public override Task<Resource> CreateAsync(Resource resource, string correlationIdentifier)
        {
            if (resource.Identifier != null)
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            Core2EnterpriseUser user = resource as Core2EnterpriseUser;
            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            IEnumerable<Core2EnterpriseUser> existingUsers = storage.Users.Values;
            if
            (
                existingUsers.Any(
                    existingUser =>
                        string.Equals(existingUser.UserName, user.UserName, StringComparison.Ordinal))
            )
            {
                throw new HttpResponseException(HttpStatusCode.Conflict);
            }

            // Update metadata
            DateTime created = DateTime.UtcNow;
            user.Metadata.Created = created;
            user.Metadata.LastModified = created; 
            
            string resourceIdentifier = Guid.NewGuid().ToString();
            resource.Identifier = resourceIdentifier;
            storage.Users.Add(resourceIdentifier, user);

            return Task.FromResult(resource);
        }

        public override Task DeleteAsync(IResourceIdentifier resourceIdentifier, string correlationIdentifier)
        {
            if (string.IsNullOrWhiteSpace(resourceIdentifier?.Identifier))
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            string identifier = resourceIdentifier.Identifier;

            if (storage.Users.ContainsKey(identifier))
            {
                storage.Users.Remove(identifier);
            }

            return Task.CompletedTask;
        }

        public override Task<Resource[]> QueryAsync(IQueryParameters parameters, string correlationIdentifier)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(nameof(correlationIdentifier));
            }

            if (null == parameters.AlternateFilters)
            {
                throw new ArgumentException(SystemForCrossDomainIdentityManagementServiceResources.ExceptionInvalidParameters);
            }

            if (string.IsNullOrWhiteSpace(parameters.SchemaIdentifier))
            {
                throw new ArgumentException(SystemForCrossDomainIdentityManagementServiceResources.ExceptionInvalidParameters);
            }

            IEnumerable<Resource> results;
            var predicate = PredicateBuilder.False<Core2EnterpriseUser>();
            Expression<Func<Core2EnterpriseUser, bool>> predicateAnd;


            if (parameters.AlternateFilters.Count <= 0)
            {
                results = storage.Users.Values.Select(
                    user => user as Resource);
            }
            else
            {

                foreach (IFilter queryFilter in parameters.AlternateFilters)
                {
                    predicateAnd = PredicateBuilder.True<Core2EnterpriseUser>();

                    IFilter andFilter = queryFilter;
                    IFilter currentFilter = andFilter;
                    do
                    {
                        if (string.IsNullOrWhiteSpace(andFilter.AttributePath))
                        {
                            throw new ArgumentException(SystemForCrossDomainIdentityManagementServiceResources.ExceptionInvalidParameters);
                        }

                        if (string.IsNullOrWhiteSpace(andFilter.ComparisonValue))
                        {
                            throw new ArgumentException(SystemForCrossDomainIdentityManagementServiceResources.ExceptionInvalidParameters);
                        }

                        // UserName filter

                        if (andFilter.AttributePath.Equals(AttributeNames.UserName, StringComparison.OrdinalIgnoreCase))
                        {
                            if (andFilter.FilterOperator != ComparisonOperator.Equals)
                            {
                                throw new NotSupportedException(
                                    string.Format(SystemForCrossDomainIdentityManagementServiceResources.ExceptionFilterOperatorNotSupportedTemplate, andFilter.FilterOperator));
                            }

                            string userName = andFilter.ComparisonValue;
                            predicateAnd = predicateAnd.And(p => string.Equals(p.UserName, userName, StringComparison.OrdinalIgnoreCase));

                           
                        }

                        // ExternalId filter
                        else if (andFilter.AttributePath.Equals(AttributeNames.ExternalIdentifier, StringComparison.OrdinalIgnoreCase))
                        {
                            if (andFilter.FilterOperator != ComparisonOperator.Equals)
                            {
                                throw new NotSupportedException(
                                    string.Format(SystemForCrossDomainIdentityManagementServiceResources.ExceptionFilterOperatorNotSupportedTemplate, andFilter.FilterOperator));
                            }

                            string externalIdentifier = andFilter.ComparisonValue;
                            predicateAnd = predicateAnd.And(p => string.Equals(p.ExternalIdentifier, externalIdentifier, StringComparison.OrdinalIgnoreCase));

                           
                        }

                        //Active Filter
                        else if (andFilter.AttributePath.Equals(AttributeNames.Active, StringComparison.OrdinalIgnoreCase))
                        {
                            if (andFilter.FilterOperator != ComparisonOperator.Equals)
                            {
                                throw new NotSupportedException(
                                    string.Format(SystemForCrossDomainIdentityManagementServiceResources.ExceptionFilterOperatorNotSupportedTemplate, andFilter.FilterOperator));
                            }

                            bool active = bool.Parse(andFilter.ComparisonValue);
                            predicateAnd = predicateAnd.And(p => p.Active == active);

                        }

                        //LastModified filter
                        else if (andFilter.AttributePath.Equals($"{AttributeNames.Metadata}.{AttributeNames.LastModified}", StringComparison.OrdinalIgnoreCase))
                        {
                            if (andFilter.FilterOperator == ComparisonOperator.EqualOrGreaterThan)
                            {
                                DateTime comparisonValue = DateTime.Parse(andFilter.ComparisonValue).ToUniversalTime();
                                predicateAnd = predicateAnd.And(p => p.Metadata.LastModified >= comparisonValue);

                               
                            }
                            else if (andFilter.FilterOperator == ComparisonOperator.EqualOrLessThan)
                            {
                                DateTime comparisonValue = DateTime.Parse(andFilter.ComparisonValue).ToUniversalTime();
                                predicateAnd = predicateAnd.And(p => p.Metadata.LastModified <= comparisonValue);

                                
                            }
                            else
                                throw new NotSupportedException(
                                    string.Format(SystemForCrossDomainIdentityManagementServiceResources.ExceptionFilterOperatorNotSupportedTemplate, andFilter.FilterOperator));



                        }
                        else
                            throw new NotSupportedException(
                                string.Format(SystemForCrossDomainIdentityManagementServiceResources.ExceptionFilterAttributePathNotSupportedTemplate, andFilter.AttributePath));

                        currentFilter = andFilter;
                        andFilter = andFilter.AdditionalFilter;

                    } while (currentFilter.AdditionalFilter != null);

                    predicate = predicate.Or(predicateAnd);

                }

                results = storage.Users.Values.Where(predicate.Compile());
            }

            if (parameters.PaginationParameters != null)
            {
                int count = parameters.PaginationParameters.Count.HasValue ? parameters.PaginationParameters.Count.Value : 0;
                return Task.FromResult(results.Take(count).ToArray());
            }

            return Task.FromResult(results.ToArray());
        }

        public override Task<Resource> ReplaceAsync(Resource resource, string correlationIdentifier)
        {
            if (resource.Identifier == null)
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            Core2EnterpriseUser user = resource as Core2EnterpriseUser;

            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            if
            (
                storage.Users.Values.Any(
                    exisitingUser =>
                        string.Equals(exisitingUser.UserName, user.UserName, StringComparison.Ordinal) &&
                        !string.Equals(exisitingUser.Identifier, user.Identifier, StringComparison.OrdinalIgnoreCase))
            )
            {
                throw new HttpResponseException(HttpStatusCode.Conflict);
            }

            Core2EnterpriseUser exisitingUser = storage.Users.Values
                .FirstOrDefault(
                    exisitingUser =>
                        string.Equals(exisitingUser.Identifier, user.Identifier, StringComparison.OrdinalIgnoreCase)
                );
            if (exisitingUser == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            // Update metadata
            user.Metadata.Created = exisitingUser.Metadata.Created;
            user.Metadata.LastModified = DateTime.UtcNow;

            storage.Users[user.Identifier] = user;
            Resource result = user;
            return Task.FromResult(result);
        }

        public override Task<Resource> RetrieveAsync(IResourceRetrievalParameters parameters, string correlationIdentifier)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(nameof(correlationIdentifier));
            }

            if (string.IsNullOrEmpty(parameters?.ResourceIdentifier?.Identifier))
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            Resource result = null;
            string identifier = parameters.ResourceIdentifier.Identifier;

            if (storage.Users.ContainsKey(identifier))
            {
                if (storage.Users.TryGetValue(identifier, out Core2EnterpriseUser user))
                {
                    result = user;
                    return Task.FromResult(result);
                }
            }

            throw new HttpResponseException(HttpStatusCode.NotFound);
        }

        public override Task UpdateAsync(IPatch patch, string correlationIdentifier)
        {
            if (null == patch)
            {
                throw new ArgumentNullException(nameof(patch));
            }

            if (null == patch.ResourceIdentifier)
            {
                throw new ArgumentException(string.Format(SystemForCrossDomainIdentityManagementServiceResources.ExceptionInvalidOperation));
            }

            if (string.IsNullOrWhiteSpace(patch.ResourceIdentifier.Identifier))
            {
                throw new ArgumentException(SystemForCrossDomainIdentityManagementServiceResources.ExceptionInvalidOperation);
            }

            if (null == patch.PatchRequest)
            {
                throw new ArgumentException(SystemForCrossDomainIdentityManagementServiceResources.ExceptionInvalidOperation);
            }

            PatchRequest2 patchRequest =
                patch.PatchRequest as PatchRequest2;

            if (null == patchRequest)
            {
                string unsupportedPatchTypeName = patch.GetType().FullName;
                throw new NotSupportedException(unsupportedPatchTypeName);
            }

            if (storage.Users.TryGetValue(patch.ResourceIdentifier.Identifier, out Core2EnterpriseUser user))
            {
                user.Apply(patchRequest);

                // Update metadata
                user.Metadata.LastModified = DateTime.UtcNow;
            }
            else
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            return Task.CompletedTask;
        }
    }
}
