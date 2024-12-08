# Notes

## Syntax analysis
L'analyse syntaxique est constituée de deux phases, la phase de lexing et de parsing. 

Le lexer consiste à prendre le fichier source et le convertir en une liste de token compréhensible pour le parser. 

Le parser permet de comprendre la syntaxe d'un document sur base des tokens récupéré par le lexer auparavant. Nous construisons à cette étape un arbre abstrait syntaxique (AST).

## Semantic analysis
L'analyse sémantique visite l'AST et vérifie tout les comportements du programme. C'est à ce moment là que nous vérifions si nous utilisons une variable non déclarée, si nous avons un return au moins dans une fonction, si les types des arguments d'un appel à une fonction correspondent aux arguments de la fonction définie.

Pour se faire nous allons utiliser une table de symboles, cette table de symbole contient tout les symboles supporté par notre langage de programmation (variables, fonctions, types, ...). Cette table de symboles nous sera utiles pour vérifier si une variable existe déjà sous le même nom, ect.

**Commençons d'abord par le type du symbole:**

```cs
public enum SymbolType
{
    Variable,
    Function,
    Class,
    Unknown
}
```

Plus tard, nous devrons composer les types de symboles 