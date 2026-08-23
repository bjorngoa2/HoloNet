# Naming parameters

Source: https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-parameters

---

# Naming Parameters

Note

This content is reprinted by permission of Pearson Education, Inc. from *Framework Design Guidelines: Conventions, Idioms, and Patterns for Reusable .NET Libraries, 2nd Edition*. That edition was published in 2008, and the book has since been fully revised in the [third edition](https://www.informit.com/store/framework-design-guidelines-conventions-idioms-and-9780135896464). Some of the information on this page may be out-of-date.

Beyond the obvious reason of readability, it is important to follow the guidelines for parameter names because parameters are displayed in documentation and in the designer when visual design tools provide Intellisense and class browsing functionality.

✔️ DO use camelCasing in parameter names.

✔️ DO use descriptive parameter names.

✔️ CONSIDER using names based on a parameter’s meaning rather than the parameter’s type.

### Naming Operator Overload Parameters

✔️ DO use `left` and `right` for binary operator overload parameter names if there is no meaning to the parameters.

✔️ DO use `value` for unary operator overload parameter names if there is no meaning to the parameters.

✔️ CONSIDER meaningful names for operator overload parameters if doing so adds significant value.

❌ DO NOT use abbreviations or numeric indices for operator overload parameter names.

*Portions © 2005, 2009 Microsoft Corporation. All rights reserved.*

*Reprinted by permission of Pearson Education, Inc. from [Framework Design Guidelines: Conventions, Idioms, and Patterns for Reusable .NET Libraries, 2nd Edition](https://www.informit.com/store/framework-design-guidelines-conventions-idioms-and-9780321545619) by Krzysztof Cwalina and Brad Abrams, published Oct 22, 2008 by Addison-Wesley Professional as part of the Microsoft Windows Development Series.*

## See also

* [Framework Design Guidelines](./)
* [Naming Guidelines](naming-guidelines)
