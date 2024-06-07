# cfcore (C Frontend Core)
As of right now, the library provides support for C99 parsing and (incomplete) preprocessing, 
GNU C keywords are reserved, but the parser ignores the `standard` parameter and can only parse pure C99 grammar.
Please note that the library is not recommended for serious use right now as no thorough testing has been done 
of the library so far, and some bugs will almost certainly be found and fixed in the near future.

## Preprocessor:
The preprocessor is currently being worked on, but is rather close to complete.
Directives supported:
- `#include`
- `#ifdef`
- `#ifndef`
- `#else`
- `#endif`
- `#define` (with token pasting, stringification and variadic arguments)
- `#undef`
- `#line`
- `#warning`
- `#error`
- `#if`
- `#elif`

Predefined macros supported:
- `__LINE__`
- `__FILE__`
- `__TIME__`
- `__TIMESTAMP__`
- `__DATE__`
- `__STDC__`
- `__STDC_HOSTED__`

## Parser:
All the features of C99 grammar should be supported. Syntax error handling might be buggy in some cases.