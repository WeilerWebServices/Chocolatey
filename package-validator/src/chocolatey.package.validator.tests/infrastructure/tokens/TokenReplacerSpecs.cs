﻿// Copyright © 2015 - Present RealDimensions Software, LLC
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// 
// You may obtain a copy of the License at
// 
// 	http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace chocolatey.package.validator.tests.infrastructure.tokens
{
    using Should;
    using validator.infrastructure.tokens;

    public class TokenReplacerSpecs
    {
        public abstract class TokenReplacerSpecsBase : TinySpec
        {
            public override void Context()
            {
            }
        }

        public class TokenReplacerTester
        {
            public string CommandName { get; set; }
            public string Version { get; set; }
        }

        public class when_using_TokenReplacer : TokenReplacerSpecsBase
        {
            private readonly TokenReplacerTester _propertyHolder = new TokenReplacerTester();
            private const string NAME = "bob";

            public override void Because()
            {
                _propertyHolder.CommandName = NAME;
            }

            [Fact]
            public void when_given_brace_brace_CommandName_brace_brace_should_replace_with_the_Name_from_the_configuration()
            {
                TokenReplacer.replace_tokens(_propertyHolder, "Hi! My name is [[CommandName]]")
                    .ShouldEqual("Hi! My name is " + NAME);
            }

            [Fact]
            public void when_given_brace_CommandName_brace_should_NOT_replace_the_value()
            {
                TokenReplacer.replace_tokens(_propertyHolder, "Hi! My name is [CommandName]")
                    .ShouldEqual("Hi! My name is [CommandName]");
            }

            [Fact]
            public void
                when_given_a_value_that_is_the_name_of_a_configuration_item_but_is_not_properly_tokenized_it_should_NOT_replace_the_value
                ()
            {
                TokenReplacer.replace_tokens(_propertyHolder, "Hi! My name is CommandName")
                    .ShouldEqual("Hi! My name is CommandName");
            }

            [Fact]
            public void when_given_brace_brace_commandname_brace_brace_should_replace_with_the_Name_from_the_configuration()
            {
                TokenReplacer.replace_tokens(_propertyHolder, "Hi! My name is [[commandname]]")
                    .ShouldEqual("Hi! My name is " + NAME);
            }

            [Fact]
            public void when_given_brace_brace_COMMANDNAME_brace_brace_should_replace_with_the_Name_from_the_configuration()
            {
                TokenReplacer.replace_tokens(_propertyHolder, "Hi! My name is [[COMMANDNAME]]")
                    .ShouldEqual("Hi! My name is " + NAME);
            }

            [Fact]
            public void if_given_brace_brace_Version_brace_brace_should_NOT_replace_with_the_Name_from_the_configuration()
            {
                TokenReplacer.replace_tokens(_propertyHolder, "Go to [[Version]]").ShouldNotContain(NAME);
            }

            [Fact]
            public void if_given_a_value_that_is_not_set_should_return_that_value_as_string_Empty()
            {
                TokenReplacer.replace_tokens(_propertyHolder, "Go to [[Version]]").ShouldEqual("Go to " + string.Empty);
            }

            [Fact]
            public void if_given_a_value_that_does_not_exist_should_return_the_original_value_unchanged()
            {
                TokenReplacer.replace_tokens(_propertyHolder, "Hi! My name is [[DataBase]]")
                    .ShouldEqual("Hi! My name is [[DataBase]]");
            }

            [Fact]
            public void if_given_an_empty_value_should_return_the_empty_value()
            {
                TokenReplacer.replace_tokens(_propertyHolder, "").ShouldEqual("");
            }

            [Fact]
            public void if_given_an_null_value_should_return_the_ll_value()
            {
                TokenReplacer.replace_tokens(_propertyHolder, null).ShouldEqual("");
            }
        }
    }
}
