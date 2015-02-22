﻿using Umbraco.Core.Persistence.DatabaseModelDefinitions;
using Umbraco.Core.Persistence.SqlSyntax;

namespace Umbraco.Core.Persistence.Migrations.Syntax.Delete.Expressions
{
    public class DeleteIndexExpression : MigrationExpressionBase
    {
        public DeleteIndexExpression(ISqlSyntaxProvider sqlSyntax, DatabaseProviders currentDatabaseProvider, DatabaseProviders[] supportedDatabaseProviders = null)
            : base(sqlSyntax, currentDatabaseProvider, supportedDatabaseProviders)
        {
            Index = new IndexDefinition();
        }

        public virtual IndexDefinition Index { get; set; }

        public override string ToString()
        {
            return string.Format(SqlSyntax.DropIndex,
                                 SqlSyntax.GetQuotedName(Index.Name),
                                 SqlSyntax.GetQuotedTableName(Index.TableName));
        }
    }
}