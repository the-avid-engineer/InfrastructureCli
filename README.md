# InfrastructureCli
A framework for building infrastructure deployment CLI.

## Out-of-the-box commands

To consume this package, you will want an executable project whose `Program.cs` file creates a new `ProgramCommand` instance. This class tages an array of `IGenerateCommand` objects, which will by available on the `new` sub-command produced by the `ProgramCommand`.

Your instances of `IGenerateCommand` are your way of generating a configurations file (one for each `IGenerateCommand`) as well as an associated template files you may want to include.


```cs
public static class Program
{
    public static Task<int> Main(string[] args)
    {
        var programCommand = new ProgramCommand(new[]
        {
            new GenerateSomeSpecificTemplateCommand(),
        });

        return programCommand.Invoke(args);
    }
}
```

Run with option `--help` to see all available commands.

---

## Copying resources with ease

You may easily copy a set of embedded files using the `EmbeddedResourcesService.Copy` method. The first argument is the directory where you want the files to be copied to. The second argument is the assembly containing the embedded resources. The third argument is an array of resource name prefixes you want to copy to the output directory. Dots (`.`) are treated as folders, and any dots after the specified prefix will be used as the folder structure when copying the files. The prefix part of the name _will not_ be included in the file structure. The exception is the _last_ dot, which will be treated as the file extension. Multiple dots in a file name are not supported by this service.

For example, if you have the following embedded resource names:

`My.Namespace.Where.Embedded.Resources.Are.NonFolderTemplate.json`
`My.Namespace.Where.Embedded.Resources.Are.FolderName.FolderTemplate.json`

And you specify:

`My.Namespace.Where.Embedded.Resources.Are.`

The resources will be copied like this:

- NonFolderTemplate.json
- FolderName
  - FolderTemplate.json

---

## Configurations File

The configurations file is a JSON file which contains all the information needed to run a deployment; multiple deployments, in fact.

```json
{
    "GlobalRegionAttributes": {},
    "GlobalAttributes": {},
    "Configurations": {
        "branch-a": {
            "RegionAttributes": {},
            "Attributes": {},
            "TemplateType": "",
            "TemplateOptions": {},
            "Template": {}
        },
        "...": {}
    }
}
```

---

## GlobalRegionAttributes

This is a dictionary, where the key is the cloud-provider specific region string, and the value is a dictionary, where the key is any string and the value is any valid JSON. More on this in the JSON Extensions. These attributes apply to _all_ configurations.

### AWS CloudFormation

For AWS CloudFormation, example keys include:
- `"us-east-1"`
- `"us-east-2"`

---

## GlobalAttributes

This is a dictionary, where the key is any string and the value is any valid JSON. More on this in the JSON Extensions. These attributes apply to _all_ configurations.

---

## Configurations

This is a dictionary, where the key is whatever you want it to be. You could use GIT branch names, for example, to configure the infrastructure per branch. The value defines what kind of deployment to perform.

---

### Template Type

Possible Values:

1. `"AwsCloudFormation"` - Specifies that the template is for AWS CloudFormation

---

### Template Options

This allows you to configure special options for the deployment, which cannot be included in the template itself. For the sake of re-usability, you probably want to use `@Fn::IncludeFile` here and specify the complete template options elsewhere. More on this in the JSON Extensions.

#### AWS CloudFormation
| Key                  | Value                        | Default        | Description                                                                                               |
|----------------------|------------------------------|----------------|-----------------------------------------------------------------------------------------------------------|
| StackName            | string                       | None, Required | The name of the CloudFormation stack.                                                                     |
| UseChangeSet         | bool                         | `false`        | Will use CreateChangeSet instead of CreateStack or UpdateStack<sup>1</sup>                                           |
| Capabilities         | string[]                     | `[]`           | Grants certain capabilities to CloudFormation while running.                                              |
| Tags                 | Dictionary<string, string>   | `{}`           | Adds tags to all resources that support stack-level tagging.                                              |
| ImportParameters     | Dictionary<string, string>   | `{}`           | Imports an Exported Output<sup>2</sup> and uses it as the value of a parameter.                           |
| ImportParameterLists | Dictionary<string, string[]> | `{}`           | Imports a set of Exported Outputs<sup>2</sup> and uses them as the values of a parameter with Type List<> |

<sup>1</sup> See [CreateStack](https://docs.aws.amazon.com/AWSCloudFormation/latest/APIReference/API_CreateStack.html),  [UpdateStack](https://docs.aws.amazon.com/AWSCloudFormation/latest/APIReference/API_UpdateStack.html), and [CreateChangeSet](https://docs.aws.amazon.com/AWSCloudFormation/latest/APIReference/API_CreateChangeSet.html)

<sup>2</sup> See [Outputs](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/outputs-section-structure.html). Outputs _must_ have an export name to be used with this feature.

---

### Template

This is the template of the deployment. For the sake of re-usability, you probably want to use `@Fn::IncludeFile` here and specify the complete template elsewhere.  More on this in the JSON Extensions.

#### AWS CloudFormation

See [User Guide](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/template-reference.html) for more information.

---

### RegionAttributes

This is a dictionary, where the key is the cloud-provider specific region string, and the value is a dictionary, where the key is any string and the value is any valid JSON. More on this in the JSON Extensions. These attributes _only apply_ to the configuration in which they are defined.

#### AWS CloudFormation

For AWS CloudFormation, example keys include:
- `"us-east-1"`
- `"us-east-2"`


---

### Attributes

This is a dictionary, where the key is any string and the value is any valid JSON. More on this in the JSON Extensions. These attributes _only apply_ to the configuration in which they are defined.

#### AWS CloudFormation

For CloudFormation, the property value should be the **Export Name** of a Stack output.

See [Outputs](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/outputs-section-structure.html) for more information.

---

## JSON Extensions

The template options and template are passed through a series of rewriters which will re-write the tree structure, making replacements along the way.

This allows you to do some basic programming in the JSON format. Functions are listed below in their order of traversal and precedence.

### Top-Down

The following functions are evaluated first, and run through the tree top-down.

#### @Fn::IncludeRawFile

If your template needs some property with a value that is essentially a file (but not a JSON file), you can separate that value into a file and include it with this function.

File names are relative to the directory of the file in which the function is invoked.

So if this is your file structure:

```
config-file.txt
```

And `config-file.txt` looks like this:

```txt
Some text configuration file with all
sorts of "symbols" and line breaks
```

Then `/template.json`:

```json
{
  "SomeProperty": {
    "@Fn::IncludeRawFile": [
      "config-file.txt"
    ]
  }
}
```

Would be re-written as:

```json
{
  "SomeProperty": "Some text configuration file with all\nsorts of \"symbols\" and line breaks"
}
```

---

#### @Fn::UsingAttributeMacro

An object with a single key of `@Fn::UsingAttributeMacro` and a value of an array with four elements, the first being the name of the attribute macro, the second being the name of the attribute arguments, and the third and fourth being anything, is recognized by this rewriter. Each element of the second element must be a string. The third element can be retrieved by using a corresponding `@Fn::GetAttributeMacro` function in the fourth element of the array. 

For example:

```json
{
    "@Fn::UsingAttributeMacro": [
        "MyAttributeMacro",
        [ "Argument1", "Argument2" ],
        "@{Argument1}-@{Argument2}"
        {
            "@Fn::GetAttributeMacro": [ "MyAttributeMacro", "A", "B" ]
        }
}
```

is equivalent to the following:

```json
{
    "@Fn::UsingAttributes": [
        {
            "Argument1": "A",
            "Argument2": "B"
        },
        "@{Arugment1}-@{Argument2}"
    ]
}
```

---

#### @Fn::GetAttributeMacro

An object with a single key of `@Fn::GetAttributeMacro` and a value of an array is recognized by this rewriter. The first element of the array must be the same as the first element of the argumnets provided to a corresponding `@Fn::UsingAttributeMacro`. The length of the array must be `1` plus the number of elements in the second element of the arguments provided to a corresponding `@Fn::UsingAttributeMacro`.

For example:

```json
{
    "@Fn::UsingAttributeMacro": [
        "MyAttributeMacro",
        [ "Argument1", "Argument2" ],
        "@{Argument1}-@{Argument2}"
        {
            "@Fn::GetAttributeMacro": [ "MyAttributeMacro", "A", "B" ]
        }
}
```

Would be rewritten as:

```json
"A-B"
```

---

#### @Fn::UsingMacros

An object with a single key of `@Fn::UsingMacros` and a value of an array with two elements, the first being an inner object and the second being anything, is recognized by this rewriter. Each property of the first element, the inner object, can be retrieved by using a corresponding `@Fn::GetMacro` function in the second element of the array.

For example:

```json
{
  "@Fn::UsingMacros": [
    {
      "MyMacro": "@{MyAttribute}"
    },
    {
      "@Fn::UsingAttributes": [
        {
          "MyAttribute": "My Attribute Value"
        },
        {
          "@Fn::GetMacro": "MyMacro"
        }
      ]
    }
  ]
]
```

Would be rewritten as:

```json
"My Attribute Value"
```

You will note that this would **not** work if you replaced `@Fn::UsingMacros` with `@Fn::UsingAttributes` and `@Fn::GetMacro` with `@Fn::GetAttributeValue` because attributes are processed bottom-up, where as macros are processed top-down; the output would be `"@{MyAttributeValue}"`

#### @Fn::GetMacro

An object with a single key of `@Fn::GetMacro` and a value of a string is recognized by this macro. Macros defined higher in the template can be accessed with this rewriter.

For example:

```json
{
  "@Fn::GetMacro": "IncludeSomeFile"
}
```

Would be rewritten as:

```json
{
  "@Fn::IncludeRawFile": ["myfile.txt"]
}
```

Assuming a macro higher in the template is defined with this as the first argument:

```json
{
  "IncludeSomeFile": {
    "@Fn::IncludeRawFile": ["myfile.txt"]
  }
}
```

### Bottom-Up

The following functions are evaluated last, and run through the tree bottom-up.

#### @Fn::GetAttributeValue (Explicit)

Attributes from the deployment configuration can be accessed with this rewriter. An object with a single key of `@Fn::GetAttributeValue` and a value of a string is recognized by this rewriter.

For example:

```json
{
    "ParentKey": {
        "@Fn::GetAttributeValue": "Foo"
    }
}
```

Would be rewritten as:

```json
{
    "ParentKey": "Bar"
}
```

Assuming that the configuration's attributes looks like this:

```json
{
    "Attributes": {
        "Foo": "Bar"
    }
}
```

Note, you are not restricted to using strings for attribute values. Any valid JSON is allowed.

#### @Fn::GetAttributeValue (Implicit)

For any attribute, consider the key of the attribute. If any occurence of `@{<key>}` occurs in the template, it will be rewritten with the string value of the attribute. (Note that this token is not valid JSON outside of a string, so it may only be used inside a string; that includes property key strings!)

For example:

```json
{
    "ParentProperty": "@{Foo}",
    "@{Foo}": "ChildValue"
}
```

Would be rewritten as:

```json
{
    "ParentProperty": "Bar",
    "Bar": "ChildValue"
}
```

WARNING: Due to the this function being processed bottom-up, you should not rely on the output of this function (the implicit version) as an argument in another function call (e.g., `@Fn::IncludeFile`) because that dependent function _might_ evaluate before the attribute value is available. In these cases, it is advised to use the explicit function. 

#### @Fn::MapElements

An object with a single key of `@Fn::MapElements` and a value of an array with two elements, the first being an array of anything and the second being anything, is recognized by this rewriter. Each element of the first element, the array, is mapped to the second element. In addition to any attributes already present, you may use the following attributes as well:

| Attribute Key | Attribute Value Type | Description                            |
|---------------|----------------------|----------------------------------------|
| ElementIndex  | number               | The index of the element being mapped. |
| ElementValue  | any                  | The value of the element being mapped. |

For example:

```json
{
    "ParentProperty": {
        "@Fn::MapElements": [
            ["a","b","c"],
            {
                "@{ElementIndex}": {
                    "@Fn::GetAttributeValue": "ElementValue"
                },
                "Index": {
                    "@Fn::GetAttributeValue": "ElementIndex"
                },
                "Value": {
                    "@Fn::GetAttributeValue": "ElementValue"
                },
                "IndexAndValue": "@{ElementIndex}:@{ElementValue}"
            }
        ]
    }
}
```

Would be rewritten as:

```json
{
    "ParentProperty": [
        {
            "0": "a",
            "Index": 0,
            "Value": "a",
            "IndexAndValue": "0:a"
        },
        {
            "1": "b",
            "Index": 1,
            "Value": "b",
            "IndexAndValue": "1:b"
        },
        {
            "2": "c",
            "Index": 2,
            "Value": "c",
            "IndexAndValue": "2:c"
        }
    ]
}
````

#### @Fn::MapProperties

An object with a single key of `@Fn::MapProperties` and a value of an array with two elements, the first being an inner object and the second being anything, is recognized by this rewriter. Each propert of the first element, the inner object, is mapped to the second element (The output of this rewriter is an array, not an object). In addition to any attributes already present, you may use the following attributes as well:

| Attribute Key | Attribute Value Type | Description                             |
|---------------|----------------------|-----------------------------------------|
| PropertyKey   | string               | The key of the property being mapped.   |
| PropertyValue | any                  | The value of the property being mapped. |

For example:

```json
{
    "ParentProperty": {
        "@Fn::MapProperties": [
            {
                "a": "alpha",
                "b": "beta",
                "c": "candy"
            },
            {
                "@{PropertyKey}": {
                    "@Fn::GetAttributeValue": "PropertyValue"
                },
                "Key": {
                    "@Fn::GetAttributeValue": "PropertyKey"
                },
                "Value": {
                    "@Fn::GetAttributeValue": "PropertyValue"
                },
                "KeyAndValue": "@{PropertyKey}:@{PropertyValue}"
            }
        ]
    }
}
```

Would be rewritten as:

```json
{
    "ParentProperty": [
        {
            "a": "alpha",
            "Key": "a",
            "Value": "alpha",
            "KeyAndValue": "a:alpha"
        },
        {
            "b": "beta",
            "Key": "b",
            "Value": "beta",
            "KeyAndValue": "b:beta"
        },
        {
            "c": "candy",
            "Key": "c",
            "Value": "candy",
            "KeyAndValue": "c:candy"
        }
    ]
}
```

#### @Fn::UsingAttributes

An object with a single key of `@Fn::UsingAttributes` and a value of an array with two elements, the first being an object and the second being anything, is recognized by this rewriter. Each property of the first element of the array can be retrieved by using a corresponding `@Fn::GetAttributeValue` function in the second element of the array.

For example:

```json
{
    "@Fn::UsingAttributes": [
        {
            "Foo": "Bar"
        },
        {
            "WhatIsFoo": {
                "@Fn::GetAttributeValue": "Foo"
            }
        }
    ]
}
```

Would be rewritten as:

```json
{
    "WhatIsFoo": "Bar"
}
```


#### @Fn::GetPropertyValue

An outer object with a single key of `@Fn::GetPropertyValue` and a value of an array with two elements, the first being an inner object and the second being a string, is recognized by this rewriter. The second element of the array, the string, is treated as a key of the first element of the array, the inner object, and the outer object is rewritten with the value of the inner object for that key.

For example:

```json
{
    "ParentKey": {
        "@Fn::GetPropertyValue":
        [
            {
                "Foo": "Bar"
            },
            "Foo"
        ]
    }
}
```

Would be rewritten as:

```json
{
    "ParentKey": "Bar"
}
```

#### @Fn::SpreadElements

An object with a single key of `@Fn::SpreadElements` and a value of an array of arrays is recognized by this rewritter. All of the arrays within the array will be combined into a single array.

For Example:

```json
{
  "@Fn::SpreadElements": [
    [
      "a",
      "b",
      "c"
    ],
    [
      "1",
      "2",
      "3"
    ]
  ]
}
```

Would be rewritten as:

```json
[
  "a",
  "b",
  "c",
  "1",
  "2",
  "3"
]
```

#### @Fn::SpreadProperties

An object with a single key of `@Fn::SpreadProperties` and a value of an array of arrays is recognized by this rewritter. All of the objects within the array will be combined into a single object.

For example:

```json
{
  "@Fn::SpreadProperties": [
    {
      "a": "a",
      "b": "b",
      "c": "c"
    },
    {
      "1": "1",
      "2": "2",
      "3": "3"
    }
  ]
}
```

Would be rewritten as:

```json
{
  "a": "a",
  "b": "b",
  "c": "c",
  "1": "1",
  "2": "2",
  "3": "3"
}
```

#### @Fn::Serialize

Sometimes you need JSON serialized inside JSON - AWS does this a lot with policies. However, writing serialized JSON inside JSON is.. ehm.. disgusting. So, this rewriter will serialize the JSON for you! An object with a key of `@Fn::Serialize` and any value is recognized by this rewritter.

For example:

```json
{
    "PolicyJson": {
        "@Fn::Serialize": {
            "Foo": "Bar"
        }
    }
}
```

Would be rewritten as:

```json
{
    "PolicyJson": "{\"Foo\":\"Bar\"}"
}
```

#### @Fn::IntProduct

An object with a single key of `@Fn::IntProduct` and a value of an array of numbers is recognized by this rewriter. The output of this function is equivalent to the PI product notation in math, and will return `1` for an empty set (a.k.a., the empty product). This function is written to handle `int` numbers, and will likely throw if anything bigger is used. (It's always possible to add a `@Fn::LongProduct` in the future if bigger numbers are needed.)

For example:

```json
{
    "@Fn::IntProduct": [1,2,3,4]
}
```

Will be rewritten as:

```json
24
```


#### @Fn::IncludeFile

If you want to split your template into components that are easier to digest/read, you may split them and recombine them using this function.

File names are relative to the directory of the file in which the function is invoked. So if this is your file structure:
```
top.json
child/child.json
child/subchild/subchild.json
```

And if `top.json` needs to include `child/child.json`, it should use:

```json
{
  "@Fn::IncludeFile": ["child", "child.json"]
}
```

While if `child/child.json` needs to include `child/subchild/subchild.json`, it should use:

```json
{
  "@Fn::IncludeFile": ["subchild", "subchild.json"]
}
```

Because the `subchild` directory is at the same level as `child.json`.

---

For example:

If you have a file `/component.json`:
```json
{
  "Foo": "Bar"
}
```

Then `/template.json`:
```json
{
  "ParentProperty": {
    "@Fn::IncludeFile": ["component.json"]
  }
}
```
Would be rewritten as:
```json
{
  "ParentProperty": {
    "Foo": "Bar"
  }
}
```

#### @Fn::IncludeFileFromPath

This function is not intended to be used directly.
