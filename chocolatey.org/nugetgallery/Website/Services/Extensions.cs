using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace NuGetGallery
{
    public static class Extensions
    {
        // Search criteria
        private static readonly Func<string, Expression<Func<Package, bool>>> idCriteria = term =>
            p => p.PackageRegistration.Id.Contains(term);

        private static readonly Func<string, Expression<Func<Package, bool>>> descriptionCriteria = term =>
            p => p.Description.Contains(term);

        private static readonly Func<string, Expression<Func<Package, bool>>> summaryCriteria = term =>
            p => p.Summary != null && p.Summary.Contains(term);

        private static readonly Func<string, Expression<Func<Package, bool>>> tagCriteria = term =>
            p => p.Tags != null && p.Tags.Contains(term);

        private static readonly Func<string, Expression<Func<Package, bool>>> authorCriteria = term =>
            p => p.Authors.Any(a => a.Name.Contains(term));

        private static readonly Func<string, Expression<Func<Package, bool>>>[] searchCriteria = new[] { 
                idCriteria, 
                descriptionCriteria,
                summaryCriteria
        };

        private static readonly Func<string, Expression<Func<Package, bool>>> idLowerCriteria = term =>
            p => p.PackageRegistration.Id.ToLower().Contains(term);

        private static readonly Func<string, Expression<Func<Package, bool>>> descriptionLowerCriteria = term =>
            p => p.Description.ToLower().Contains(term);

        private static readonly Func<string, Expression<Func<Package, bool>>> summaryLowerCriteria = term =>
            p => p.Summary != null && p.Summary.ToLower().Contains(term);

        private static readonly Func<string, Expression<Func<Package, bool>>> tagLowerCriteria = term =>
            p => p.Tags != null && p.Tags.ToLower().Contains(term);

        private static readonly Func<string, Expression<Func<Package, bool>>> authorLowerCriteria = term =>
            p => p.Authors.Any(a => a.Name.ToLower().Contains(term));

        private static readonly Func<string, Expression<Func<Package, bool>>>[] searchLowerCriteria = new[] { 
                idLowerCriteria, 
                descriptionLowerCriteria,
                summaryLowerCriteria
        };

        public static IQueryable<Package> Search(this IQueryable<Package> source, string searchTerm, bool lowerCaseExpression = true)
        {
            if (String.IsNullOrWhiteSpace(searchTerm))
            {
                return source;
            }

            // Split the search terms by spaces
            var terms = (searchTerm ?? String.Empty).Split();

            var idSearch = searchTerm.to_lower().Contains("id:");
            var authorSearch = searchTerm.to_lower().Contains("author:");
            var tagSearch = searchTerm.to_lower().Contains("tag:");

            // Build a list of expressions for each term
            var expressions = new List<LambdaExpression>();
            foreach (var term in terms)
            {
                // doesn't matter if this is lowercased or not
                var localSearchTerm = term.to_lower().Replace("id:", string.Empty).Replace("author:", string.Empty).Replace("tag:", string.Empty);

                if (idSearch)
                {
                    expressions.Add(lowerCaseExpression ?
                        idLowerCriteria(localSearchTerm)
                        : idCriteria(localSearchTerm)
                    );
                }
                else if (authorSearch)
                {
                    expressions.Add(lowerCaseExpression ?
                        authorLowerCriteria(localSearchTerm)
                        : authorCriteria(localSearchTerm)
                    );
                }
                else if (tagSearch)
                {
                    expressions.Add(lowerCaseExpression ?
                        tagLowerCriteria(localSearchTerm)
                        : tagCriteria(localSearchTerm)
                    );
                }
                else
                {
                    var criteriaList = lowerCaseExpression ? searchLowerCriteria : searchCriteria;

                    foreach (var criteria in criteriaList)
                    {
                        expressions.Add(criteria(localSearchTerm));
                    }
                }
            }

            //todo this becomes an AND
            // Build a giant or statement using the bodies of the lambdas
            var body = expressions.Select(p => p.Body)
                                  .Aggregate(Expression.OrElse);

            // Now build the final predicate
            var parameterExpr = Expression.Parameter(typeof(Package));

            // Fix up the body to use our parameter expression
            body = new ParameterExpressionReplacer(parameterExpr).Visit(body);

            // Build the final predicate
            var predicate = Expression.Lambda<Func<Package, bool>>(body, parameterExpr);

            // Apply it to the query
            return source.Where(predicate);
        }

        private class ParameterExpressionReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _parameterExpr;
            public ParameterExpressionReplacer(ParameterExpression parameterExpr)
            {
                _parameterExpr = parameterExpr;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (node.Type == _parameterExpr.Type &&
                    node != _parameterExpr)
                {
                    return _parameterExpr;
                }
                return base.VisitParameter(node);
            }
        }
    }
}