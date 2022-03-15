# InfrastructureCli
A framework for building infrastructure deployment CLI.

## Configurations File

The configurations file is a JSON file which contains all the information needed to run a deployment; multiple deployments, in fact.

```json
{
    "Configurations": {
        "branch-a": {
            "TemplateType": "",
            "TemplateOptions": {},
            "Template": {},
            "Label": "....",
            "Attributes": {},
            "Tags": {},
            "PropertyMaps": {}
        },
        "...": {}
    }
}
```

## Configurations

This is a dictionary, where the key is whatever you want it to be. You could use GIT branch names, for example, to configure the infrastructure per branch. The value defines what kind of deployment to perform.

### Template Type

Possible Values:

1. `"AwsCloudFormation"` - Specifies that the template is for AWS Cloud Formation

---

### Template Options

This allows you to configure special options for the deployment, which cannot be included in the template itself.

#### AWS Cloud Formation

- `Capabilities` if specifies should be `string[]`

---

### Template

This is the template of the deployment. For the sake of re-usability, you probably want to use `@Fn::IncludeFile` here and specify the complete template elsewhere.  More on this in the Template File Extensions.

#### AWS Cloud Formation

See [AWS CloudFormation User Guide](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/template-reference.html) for basic information on AWS CloudFormation.

---

### Label

You are free to choose whatever you want here. It should generally be unique, and describe the deployment at a high level.

#### AWS Cloud Formation

This is used as the stack name of the deployment.

---

### Attributes

This is a dictionary, where the key is a string and the value is any valid JSON. More on this in the Template File Extensions.

---

### Tags

This is a dictionary, where the key and value are strings. Implementation is up to the deployer, but ideally these tags are applied to every resource in the infrastructure deployment. ** This may be moved into the `TemplateOptions` property at a future time. It used to make sense as its own thing, but not any more. **


## Template File Extensions

The template file is a JSON file which codifies the infrastructure. The template file is passed through a rewriter which will re-write the structure, and allows you to do some basic programming in JSON format. Functions are listed in order of precedence, and the template is processed bottom-up.

### @Fn::GetAttributeValue (Explicit)

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

### @Fn::GetAttributeValue (Implicit)

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

WARNING: Due to the template being processed bottom-up, you should not rely on the output of this function (the implicit version) as an argument in another function call (e.g., `@Fn::IncludeFile`) because that dependent function _might_ evaluate before the attribute value is available. In these cases, it is advised to use the explicit function. 

### @Fn::MapElements

An object with a single key of `@Fn::MapElements` and a value of an array with two elements, the first being an array of anything and the second being anything, is recognized by this rewriter. Each element of the first element, the array, is mapped to the second element. In addition to any attributes already present, you may use the following attributes as well:

| Attribute Key | Attribute Value Type | Description                            |
|---------------|----------------------|----------------------------------------|
| ElementIndex  | number               | The index of the element being mapped. |
| ElementValue  | any                  | The value of the element being mapped. |

For example:

```json
{
    "ParentProperty": {
        "@Fn::Map": [
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

### @Fn::MapProperties

An object with a single key of `@Fn::MapProperties` and a value of an array with two elements, the first being an inner object and the second being anything, is recognized by this rewriter. Each propert of the first element, the inner object, is mapped to the second element (The output of this rewriter is an array, not an object). In addition to any attributes already present, you may use the following attributes as well:

| Attribute Key | Attribute Value Type | Description                             |
|---------------|----------------------|-----------------------------------------|
| PropertyKey   | string               | The key of the property being mapped.   |
| PropertyValue | any                  | The value of the property being mapped. |

For example:

```json
{
    "ParentProperty": {
        "@Fn::Map": [
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

### @Fn::UsingAttributes

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


### @Fn::GetPropertyValue

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

### @Fn::SpreadElements

An object with a single key of `@Fn::SpreadElements` and a value of an array of arrays is recognized by this rewritter. All of the arrays within the array will be combined into a single array.

For Example:

```json
{
  "@Fn::Spread": [
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

### @Fn::SpreadProperties

An object with a single key of `@Fn::SpreadProperties` and a value of an array of arrays is recognized by this rewritter. All of the objects within the array will be combined into a single object.

For example:

```json
{
  "@Fn::Spread": [
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

### @Fn::Serialize

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

### @Fn::IntProduct

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


### @Fn::IncludeFile

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
