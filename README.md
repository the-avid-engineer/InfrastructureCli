# InfrastructureCli
A framework for building infrastructure deployment CLI.

## Configurations File

The configurations file is a JSON file which indicates what kind of deployment it is for (e.g., AwsCloudFormation), and has multiple ways of configuring the infrastructure.

```
{
    "Type": "...",
    "Configurations": {
        "....": {
            "Label": "....",
            "Attributes": { ... },
            "Tags": {},
            "Meta": {}
        },
        ...
    }
}
```

`Type` can be `AwsCloudFormation` - more options may come in the future.

`Configurations` is a dictionary, where the key is whatever you want it to be. You could use GIT branch names, for example, to configure the infrastructure per branch.

`Label` is a label for the specific configuration. This might be the application name and the environment name combined together.

`Attributes` is dictionary, where the key is a string and the value is any valid JSON. More on this in the Template File section.

`Tags` is a dictionary, where the key and value are strings. Implementation is up to the deployer, but ideally these tags are applied to every resource in the infrastructure deployment. You can retrieve information from this dictionary using `get <configuration key> tag <tag key>`

`Meta` is a dictionary, where the key and value are strings - and contain any other information about the configuration. The AWS CloudFormation deployment expects an `AccountId` and `Region` entry, to ensure that a deployment always goes to the same place. You can retrieve information from this dictionary using `get <configuration key> meta <meta key>`

## Template File

The template file is a JSON file which codifies the infrastructure. The template file is passed through a rewriter which will re-write the structure, and allows you to do some basic programming in JSON format. Functions are listed in order of precedence.

### @GetAttributeValue (Explicit)

Attributes from the deployment configuration can be accessed with this rewriter. An object with a single key of `@GetAttributeValue` and a value of a string is recognized by this rewriter.

For example:

```
{
    "ParentKey": {
        "@GetAttributeValue": "Foo"
    }
}
```

Would be rewritten as:

```
{
    "ParentKey": "Bar"
}
```

Assuming that the configuration's attributes looks like this:

```
{
    "Attributes": {
        "Foo": "Bar"
    }
}
```

Note, you are not restricted to using strings for attribute values. Any valid JSON is allowed.

### @GetAttributeValue (Implicit)

For any attribute, consider the key of the attribute. If any occurence of `@{<key>}` occurs in the template, it will be rewritten with the stringified value of the attribute.

For example:

```
{
    "ParentProperty": "@{Foo}",
    "@{Foo}": "ChildValue"
}
```

Would be rewritten as:

```
{
    "ParentProperty": "Bar",
    "Bar": "ChildValue"
}
```

### @MapElements

An object with a single key of `@MapElements` and a value of an array with two elements, the first being an array of anything and the second being anything, is recognized by this rewriter. Each element of the first element, the array, is mapped to the second element. In addition to any attributes already present, you may use the following attributes as well:

| Attribute Key | Attribute Value Type | Description                            |
|---------------|----------------------|----------------------------------------|
| ElementIndex  | number               | The index of the element being mapped. |
| ElementValue  | any                  | The value of the element being mapped. |

For example:

```
{
    "ParentProperty": {
        "@Map": [
            ["a","b","c"],
            {
                "@{ElementIndex}": {
                    "@GetAttributeValue": "ElementValue"
                },
                "Index": {
                    "@GetAttributeValue": "ElementIndex"
                },
                "Value": {
                    "@GetAttributeValue": "ElementValue"
                },
                "IndexAndValue": "@{ElementIndex}:@{ElementValue}"
            }
        ]
    }
}
```

Would be rewritten as:

```
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

### @MapProperties

An object with a single key of `@MapProperties` and a value of an array with two elements, the first being an inner object and the second being anything, is recognized by this rewriter. Each propert of the first element, the inner object, is mapped to the second element (The output of this rewriter is an array, not an object). In addition to any attributes already present, you may use the following attributes as well:

| Attribute Key | Attribute Value Type | Description                             |
|---------------|----------------------|-----------------------------------------|
| PropertyKey   | string               | The key of the property being mapped.   |
| PropertyValue | any                  | The value of the property being mapped. |

For example:

```
{
    "ParentProperty": {
        "@Map": [
            {
                "a": "alpha",
                "b": "beta",
                "c": "candy"
            },
            {
                "@{PropertyKey}": {
                    "@GetAttributeValue": "PropertyValue"
                },
                "Key": {
                    "@GetAttributeValue": "PropertyKey"
                },
                "Value": {
                    "@GetAttributeValue": "PropertyValue"
                },
                "KeyAndValue": "@{PropertyKey}:@{PropertyValue}"
            }
        ]
    }
}
```

Would be rewritten as:

```
{
    "ParentProperty": [
        {
            "a": "alpha"
            "Key": "a",
            "Value": "alpha",
            "KeyAndValue": "a:alpha"
        },
        {
            "b": "beta"
            "Key": "b",
            "Value": "beta",
            "KeyAndValue": "b:beta"
        },
        {
            "c": "candy"
            "Key": "c",
            "Value": "candy",
            "KeyAndValue": "c:candy"
        }
    ]
}
```

### @UsingAttributes

An object with a single key of `@UsingAttributes` and a value of an array with two elements, the first being an object and the second being anything, is recognized by this rewriter. Each property of the first element of the array can be retrieved by using a corresponding `@GetAttributeValue` function in the second element of the array.

For example:

```
{
    "@UsingAttributes": [
        {
            "Foo": "Bar"
        },
        {
            "WhatIsFoo": {
                "@GetAttributeValue": "Foo"
            }
        }
    ]
}
```

Would be rewritten as:

```
{
    "WhatIsFoo": "Bar"
}
```


### @GetPropertyValue

An outer object with a single key of `@GetPropertyValue` and a value of an array with two elements, the first being an inner object and the second being a string, is recognized by this rewriter. The second element of the array, the string, is treated as a key of the first element of the array, the inner object, and the outer object is rewritten with the value of the inner object for that key.

For example:

```
{
    "ParentKey": {
        "@GetPropertyValue":
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

```
{
    "ParentKey": "Bar"
}
```

### @SpreadElements

An object with a single key of `@SpreadElements` and a value of an array of arrays is recognized by this rewritter. All of the arrays within the array will be combined into a single array.

For Example:

```
{
  "@Spread": [
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

```
[
  "a",
  "b",
  "c",
  "1",
  "2",
  "3"
]
```

### @SpreadProperties

An object with a single key of `@SpreadProperties` and a value of an array of arrays is recognized by this rewritter. All of the objects within the array will be combined into a single object.

For example:

```
{
  "@Spread": [
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

```
{
  "a": "a",
  "b": "b",
  "c": "c",
  "1": "1",
  "2": "2",
  "3": "3"
}
```

### @Serialize

Sometimes you need JSON serialized inside JSON - AWS does this a lot with policies. However, writing serialized JSON inside JSON is.. ehm.. disgusting. So, this rewriter will serialize the JSON for you! An object with a key of `@Serialize` and any value is recognized by this rewritter.

For example:

```
{
    "PolicyJson": {
        "@Serialize": {
            "Foo": "Bar"
        }
    }
}
```

Would be rewritten as:

```
{
    "PolicyJson": "{\"Foo\":\"Bar\"}"
}
```

### @IntProduct

An object with a single key of `@IntProduct` and a value of an array of numbers is recognized by this rewriter. The output of this function is equivalent to the PI product notation in math, and will return `1` for an empty set (a.k.a., the empty product). This function is written to handle `int` numbers, and will likely throw if anything bigger is used. (It's always possible to add a `@LongProduct` in the future if bigger numbers are needed.)

For example:

```
{
    "@IntProduct": [1,2,3,4]
}
```

Will be rewritten as:

```
24
```