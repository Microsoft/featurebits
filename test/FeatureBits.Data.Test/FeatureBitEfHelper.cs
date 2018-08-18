﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using FeatureBits.Data.EF;
using Microsoft.EntityFrameworkCore;

namespace FeatureBits.Data.Test
{
    public class FeatureBitEfHelper
    {
        public static Tuple<FeatureBitsEfDbContext, DbContextOptions<FeatureBitsEfDbContext>> SetupDbContext()
        {
            string guidDbNameForUniqueness = Guid.NewGuid().ToString();
            DbContextOptions<FeatureBitsEfDbContext> options = new DbContextOptionsBuilder<FeatureBitsEfDbContext>()
                .UseInMemoryDatabase(guidDbNameForUniqueness)
#if !NET452
                .EnableSensitiveDataLogging(false)
#endif
                .Options;
            var dbContext = new FeatureBitsEfDbContext(options);
            return new Tuple<FeatureBitsEfDbContext, DbContextOptions<FeatureBitsEfDbContext>>(dbContext, options);
        }
    }
}