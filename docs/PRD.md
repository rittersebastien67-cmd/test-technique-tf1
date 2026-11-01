# PRD — Gestion des demandes de congé avec agent IA

> Cadrage **fonctionnel** de la User Story 1, avec une projection vers la V2. Les choix techniques (architecture, LLM, persistance) font l'objet d'un document séparé.
>
> Version 1.4 du 01/11/2026. La V1 couvre les quatre types de congé de l'énoncé ; les congés familiaux et de parentalité, cadrés en version 1.1, sont reportés en V2 (D51). Les versions 1.1 et 1.2 ont aussi ajouté un employé à temps partiel (D36 à D40) et le calendrier férié d'Alsace-Moselle (D44 à D50). La version 1.4 aligne le PRD sur l'Architecture Brief (D52 à D58) : pas de contrat de travail, fiche RH réduite à l'identité, agent sans consultation, rien n'est supprimé.

## Sommaire
1. [Contexte et objectifs](#1-contexte-et-objectifs)
2. [Acteurs](#2-acteurs)
3. [Langage ubiquitaire](#3-langage-ubiquitaire)
4. [Règles métier V1](#4-règles-métier-v1)
5. [Cycle de vie d'une demande](#5-cycle-de-vie-dune-demande)
6. [Parcours utilisateur V1](#6-parcours-utilisateur-v1)
7. [Conformité RGPD et AI Act](#7-conformité-rgpd-et-ai-act)
8. [Hors périmètre V1](#8-hors-périmètre-v1)
9. [Pistes V2](#9-pistes-v2)
10. [Journal des décisions](#10-journal-des-décisions)

---

## 1. Contexte et objectifs

**User Story 1 — Soumettre une demande de congé.**
L'employé veut soumettre une demande de congé dans l'application RH pour planifier ses vacances ou d'autres absences. Pour gagner du temps, il peut aussi décrire sa demande en langage naturel : un agent IA la prépare alors à partir des informations dont dispose l'application.

**Types de congé.** La V1 couvre les quatre types cités par l'énoncé : CP, RTT, Maladie et Sans solde. Les congés légaux familiaux et de parentalité (événements familiaux, deuil, maternité, paternité, adoption, congé parental d'éducation, enfant malade) sont des pistes V2 (9, D51).

**Objectifs V1**
- Préparer une demande par deux canaux : un formulaire avec calendrier, ou un agent conversationnel.
- Appliquer toutes les règles métier de la même manière, quel que soit le canal.
- Garantir que **la soumission reste toujours un geste explicite de l'employé**.
- Respecter le RGPD et l'AI Act dès la V1.

**Objectif V2** : préparer l'arrivée d'un workflow d'approbation, de notifications et d'une montée en charge, sans remettre en cause le modèle V1.

---

## 2. Acteurs
| Acteur | Version | Rôle |
|---|---|---|
| **Employé** | V1 | Prépare, soumet, consulte et annule ses demandes. Il n'y a pas d'authentification : l'employé courant est choisi parmi des employés de démonstration. |
| **Agent IA** | V1 | Prépare un brouillon à partir d'une conversation. Il ne soumet jamais et n'annule jamais. |
| **Manager** | V2 | Approuve ou refuse les demandes. |
| **RH / Administrateur** | V2 | Paramètre les contrats, les fiches RH, les compteurs et les calendriers. |

---

## 3. Langage ubiquitaire

Les documents fonctionnels restent en français. Dans le code, chaque terme a un équivalent anglais, défini dans la correspondance de l'Architecture Brief (`ARCHITECTURE.md`, §3.0) (D57).

### 3.1 Employé et profil
| Terme | Définition |
|---|---|
| **Employé** | Personne qui pose des congés. Il a une fiche RH et des compteurs. |
| **Fiche RH** | Données de l'employé : son **identité** (prénom, nom, poste, manager direct), son **horaire** et le département de son **lieu de travail**. Seule l'identité est affichée ; l'horaire et le lieu de travail servent au décompte (R2) et au calendrier férié (R11). La fiche vient des données de démo et n'est pas modifiable en V1. Elle ne contient pas de contrat de travail (D52, D53). |
| **Manager direct** | Prénom et nom du responsable de l'employé. C'est une information affichée, sans rôle en V1 ; le workflow d'approbation est une piste V2. |
| **Horaire** | Jours travaillés de la semaine : du lundi au vendredi, ou lundi, mardi, jeudi et vendredi pour le temps partiel 80 %. |
| **Lieu de travail** | Département de l'établissement où travaille l'employé. Il détermine son calendrier férié (R11). |
| **Profil de temps de travail** | Décrit les compteurs fournis par les données de démo pour un employé. Ce n'est pas un contrat modélisé : il n'y a ni modalité contractuelle ni date d'entrée dans l'application (D52). Il existe quatre profils, et seulement quatre. |
| · *Horaire 35 h* | Durée légale. Compteur RTT à **0 j**. |
| · *Horaire 40 h* | Compteur de **10 j de RTT** par an. |
| · *Forfait jours (218 j/an)* | Compteur de **JRTT**, fixé dans les données de démo : 11 j en 2027, et 10 j en Alsace-Moselle, où le Vendredi saint tombe le vendredi 26/03/2027. Ces valeurs viennent de la formule légale : 365 − jours de week-end − 25 CP − jours fériés tombant un jour ouvré − 218. L'application ne la calcule pas (D52). |
| · *Temps partiel 80 %* | 28 h par semaine (base 35 h), travaillées le lundi, le mardi, le jeudi et le vendredi ; le mercredi n'est pas travaillé. Compteur RTT à **0 j** ; mêmes 25 j de CP qu'un temps plein. |

### 3.2 Demande de congé
| Terme | Définition |
|---|---|
| **Demande de congé** | Intention d'absence d'un employé. Elle comprend une période, un type, un commentaire optionnel, un statut et une origine. |
| **Période d'absence** | Couple (**borne de début**, **borne de fin**). Chaque borne est une date et une demi-journée. |
| **Demi-journée** | *Matin* ou *Après-midi*. C'est la plus petite unité qu'on peut poser (0,5 j). |
| **Type de congé** | *Congés payés (CP)*, *RTT* (RTT ou JRTT selon le profil), *Maladie*, *Sans solde*. |
| **Type à solde** | CP et RTT. Leur pose est limitée par un compteur. Maladie et Sans solde ne le sont pas. |
| **Commentaire** | Texte libre optionnel de 200 caractères au plus. Il ne doit contenir aucun motif médical. Il n'est pas proposé pour la Maladie (R6). |
| **Statut** | *Brouillon*, *Soumise*, *Annulée* (par l'employé, qu'il s'agisse d'un brouillon ou d'une demande soumise) ou *Remplacée* (entièrement écrasée par un arrêt maladie). La V2 ajoute *Approuvée* et *Refusée*. Une demande n'est jamais supprimée (C17). |
| **Origine** | *Saisie manuelle* ou *Proposée par l'agent*. |
| **Soumission** | Geste explicite de l'employé qui fait passer un brouillon au statut *Soumise*, après contrôle complet des règles. |
| **Annulation** | Retrait par l'employé d'un brouillon ou d'une demande soumise. Pour une demande soumise, elle libère le solde et la période (R7). La demande reste conservée avec le statut *Annulée* (D56). |

### 3.3 Calendrier et décompte
| Terme | Définition |
|---|---|
| **Jour ouvré** | Du lundi au vendredi, hors jours fériés. C'est la seule unité décomptée ; les jours ouvrables ne sont pas utilisés. |
| **Jour férié** | Jour férié légal du calendrier férié de l'employé (R11). |
| **Calendrier férié** | Liste des jours fériés qui s'appliquent à un employé selon son lieu de travail. Le *calendrier général* compte les 11 jours fériés légaux. Le *calendrier Alsace-Moselle* (Bas-Rhin, Haut-Rhin, Moselle) ajoute le Vendredi saint et le 26 décembre (Saint-Étienne), soit 13 jours. Les jours chômés d'usage ne sont pas des jours fériés en V1. |
| **Jour travaillé** | Jour où l'employé travaille selon son horaire. |
| **Jour non travaillé** | Jour ouvré que son horaire à temps partiel ne prévoit pas (ici, le mercredi). |
| **Reprise** | Premier jour travaillé après la période d'absence. |
| **Durée décomptée** | Nombre de jours ouvrés de la période, par pas de 0,5. |
| **Chevauchement** | Situation où deux demandes *soumises* du même employé partagent au moins une demi-journée. Il est interdit, sauf pour un arrêt maladie (écrasement). |
| **Écrasement** | Effet d'un arrêt maladie soumis sur les congés déjà posés pendant sa période : les demi-journées communes sont retirées des congés et rendues au compteur. |
| **Déclaration tardive** | Arrêt maladie déclaré plus de 48 h après son début. |

### 3.4 Compteurs
| Terme | Définition |
|---|---|
| **Compteur** | Existe pour chaque employé et chaque type à solde (CP, RTT). Il comprend les **droits acquis**, les **jours réservés** par les demandes soumises et le **solde disponible** (acquis − réservés). Les jours réservés se répartissent en jours **pris** (période passée ou en cours) et jours **planifiés** (période à venir). |
| **Réservation** | Débit du compteur à la soumission. Une annulation le recrédite. |
| **Année d'utilisation des JRTT** | Année civile en cours. Les JRTT ne peuvent être posés que sur cette année, et le reliquat n'est pas reporté. |

### 3.5 Agent
| Terme | Définition |
|---|---|
| **Conversation** | Échange entre l'employé et l'agent. Elle est rattachée à **un seul** brouillon. |
| **Tour** | Un message de l'employé suivi de la réponse de l'agent. Une conversation compte au plus 10 tours. |
| **Proposition** | État du brouillon produit par l'agent à un tour donné. |
| **Signalement** | Remarque de l'agent attachée à un champ. Une *incertitude* signale une hypothèse ; une *incohérence* signale une règle violée ou une contradiction dans la demande. |
| **Message de refus standard** | Réponse fixe de l'agent quand le message est hors sujet ou cherche à détourner l'agent. |

---

## 4. Règles métier V1

| # | Règle | Détail |
|---|---|---|
| **R1** | Période valide | La borne de début est ≤ la borne de fin. Sur un même jour, Matin→Matin et Après-midi→Après-midi valent 0,5 j, Matin→Après-midi vaut 1 j, et Après-midi→Matin est invalide. |
| **R2** | Décompte | Seuls les jours ouvrés comptent, par pas de 0,5 j. Une durée décomptée nulle est invalide. **Temps partiel** : pour les CP, le décompte légal s'applique. Il va du premier jour qui aurait été travaillé jusqu'à la veille de la reprise, et tous les jours ouvrés de cet intervalle comptent, y compris les jours non travaillés (exemples en 4.1). Un jour non travaillé n'est décompté que si le jour travaillé qui le précède est posé **en entier** : une demi-journée ne s'étend pas au jour non travaillé suivant (D40). Une période qui ne couvre que des jours non travaillés compte 0 j : elle est invalide. Pour la Maladie et le Sans solde, seuls les jours travaillés sont décomptés (D39). |
| **R3** | Solde | Pour les CP et les RTT, la durée décomptée doit être ≤ au solde disponible. |
| **R4** | Chevauchement | Deux demandes soumises d'un même employé ne peuvent pas se chevaucher, à la demi-journée près : poser le matin puis l'après-midi du même jour est autorisé. Les brouillons et les demandes annulées ou remplacées ne comptent pas. **Exception** : un arrêt maladie peut chevaucher des congés déjà posés et les écrase (R10). |
| **R5** | Pas d'antériorité | La borne de début doit être ≥ à aujourd'hui, **sauf pour la Maladie**, qui peut commencer dans le passé. Au-delà de 48 h après le début, la demande est **acceptée** avec un avertissement *déclaration tardive* : l'application enregistre l'absence réelle, et le respect du délai reste une question RH. |
| **R6** | Commentaire | 200 caractères au plus. Aucun commentaire pour la Maladie (C15). |
| **R7** | Annulation | Possible tant que le congé n'a pas commencé. Elle recrédite le compteur. |
| **R8** | Données de démonstration | Les droits viennent des données de démo, conformes au droit français : 25 jours ouvrés de CP par an pour tous, temps partiel compris ; 0 RTT en horaire 35 h et à temps partiel 80 % ; 10 RTT en horaire 40 h ; JRTT fixés dans les données de démo pour le forfait 218 j (3.1), sans calcul par l'application (D52). Chaque profil a au moins un employé de démo, avec des soldes déjà entamés. Chaque employé de démo a une fiche RH. Deux employés de démo servent des cas précis. **`alsace-moselle`** (forfait 218 j) travaille en Moselle : ses JRTT 2027 sont de 10 j, contre 11 j avec le calendrier général (R11). **`temps-partiel-80`** porte le profil à temps partiel (25 j de CP, 0 RTT). En V1, chaque type à solde a un seul compteur, sans période de référence ni acquisition mensuelle. |
| **R9** | Année des JRTT | Pour un compteur JRTT (profil forfait jours), une demande de RTT doit se situer entièrement dans l'**année civile en cours**. Le 01/11/2026, un JRTT en août 2027 est refusé, tout comme un JRTT du 30/12/2026 au 04/01/2027. |
| **R10** | La maladie écrase les congés | Un arrêt maladie soumis l'emporte sur les demandes soumises de type CP, RTT ou Sans solde qu'il chevauche, même si elles ont déjà commencé. Seules les demi-journées communes sont retirées : une demande couverte en partie est **raccourcie**, ou **scindée en deux** si l'arrêt tombe au milieu ; une demande couverte entièrement passe au statut *Remplacée*. Les jours retirés sont **recrédités** sur le compteur concerné. Avant de soumettre, l'employé voit les congés qui seront écrasés et le nombre de jours recrédités. Deux arrêts maladie ne peuvent toujours pas se chevaucher. Les brouillons ne sont pas touchés. |
| **R11** | Jours fériés selon le lieu de travail (art. L3133-1 et L3134-13) | Le calendrier férié d'un employé dépend du lieu de travail inscrit sur sa fiche RH. Le calendrier général compte 11 jours. Celui d'Alsace-Moselle en compte 13, avec le Vendredi saint et le 26 décembre. Les jours chômés d'usage (Mardi gras, Mercredi des Cendres…) restent des jours ouvrés en V1 (D48). La loi réserve le Vendredi saint aux communes qui ont un temple protestant ou une église mixte ; la V1 l'applique aux trois départements entiers (D45). Ce calendrier sert au décompte des jours ouvrés (R2), au calcul des JRTT (3.1) et à l'affichage du calendrier (6.3). |

Les règles R1 à R6 et R9 à R11 sont **contrôlées à la soumission**. La déclaration tardive (R5) ne produit qu'un avertissement, qui n'empêche pas la soumission. Un brouillon peut être incomplet ou invalide ; il porte alors ses signalements.

### 4.1 Décompte des CP à temps partiel (R2)
Exemples pour l'employé à temps partiel 80 %, qui ne travaille pas le mercredi :

| CP posés | Jours décomptés | Explication |
|---|---|---|
| Du lundi au vendredi | 5 j | Le mercredi compte. |
| Mardi seul | 2 j | La reprise a lieu le jeudi, donc le mercredi compte. |
| Du mercredi au vendredi | 2 j | Le décompte démarre le jeudi, premier jour qui aurait été travaillé. |
| Mercredi seul | — | Invalide : 0 j décompté. |
| Mardi après-midi | 0,5 j | Le mardi n'est pas posé en entier, donc le mercredi ne compte pas (D40). |

---

## 5. Cycle de vie d'une demande

```
            enregistrer / agent
                   │
                   ▼
             ┌───────────┐  annuler
             │ Brouillon │ ──────────► Annulée
             └─────┬─────┘
                   │ soumettre (geste manuel, règles R1–R6, R9–R11)
                   │ → réservation du solde
                   ▼
             ┌───────────┐  annuler (R7) → recrédit du solde
             │  Soumise  │ ──────────► Annulée
             │           │  écrasée en entier par un arrêt maladie (R10)
             │           │ ──────────► Remplacée
             └─────┬─────┘  (écrasement partiel : reste Soumise, raccourcie ou scindée)
                   │ V2
                   ▼
           Approuvée | Refusée
```

- Un **Brouillon** peut être modifié et annulé.
- **Aucune demande n'est supprimée**, quel que soit son statut. Chaque action est conservée avec son auteur, employé ou agent (C17).
- En **V2**, le solde est consommé à l'approbation et recrédité au refus.

---

## 6. Parcours utilisateur V1

### 6.1 Deux canaux pour une même soumission
L'employé peut préparer une demande de deux façons, mais il n'y a **qu'une seule façon de la soumettre** :
- avec l'**interface**, en choisissant les dates dans un calendrier (6.3) ;
- avec l'**agent**, qui prépare un brouillon (6.4).

Dans les deux cas, la **soumission finale est un geste manuel** de l'employé, sur le brouillon ouvert dans le formulaire. Les règles contrôlées sont les mêmes quel que soit le canal.

### 6.2 Tableau de bord
L'employé y trouve :
- son identité : prénom, nom, poste et manager direct (D54) ;
- ses compteurs CP et RTT/JRTT : acquis, réservés (dont pris et planifiés), disponible ;
- ses demandes (brouillons, soumises, annulées), avec leur origine et leur durée décomptée.

### 6.3 Saisie manuelle avec le calendrier
1. L'employé choisit la borne de début puis la borne de fin dans un **calendrier**, en précisant à chaque fois la demi-journée. Le calendrier met en évidence les week-ends et les jours fériés du calendrier férié de l'employé (non décomptés, R11), ainsi que les périodes déjà posées. Pour un employé à temps partiel, les jours non travaillés sont hachurés ; une info-bulle explique qu'ils sont décomptés en CP lorsqu'ils précèdent la reprise (R2).
2. Il renseigne le type et le commentaire, qui n'est pas proposé pour la Maladie (R6). La durée décomptée, avec son détail jour par jour, et le solde après demande s'affichent en direct.
3. Il peut **enregistrer un brouillon** ou **soumettre**.
4. Si la soumission échoue, une erreur s'affiche sous chaque champ concerné, avec un message explicite. Par exemple : « Solde RTT insuffisant : 3 j demandés, 2 j disponibles » ou « Chevauche la demande du 12/08 au 14/08 ».
   Pour un arrêt maladie qui chevauche des congés, ce n'est pas une erreur : un message de confirmation s'affiche, par exemple « Cet arrêt remplace 2 j de CP du 13/08 au 14/08 ; ils vous seront recrédités » (R10).
5. Depuis la liste, l'employé peut reprendre ou annuler un brouillon, et annuler une demande soumise (R7). Rien n'est supprimé (C17).

### 6.4 Saisie avec l'agent (10 tours au plus)
1. L'employé ouvre une conversation et décrit son besoin.
2. L'agent connaît la date du jour, mais il ne consulte ni la fiche RH, ni les compteurs, ni les demandes de l'employé (D55). Il crée ou met à jour le brouillon. À chaque fois, l'application simule la demande et lui renvoie la durée décomptée (jours fériés et temps partiel compris, R2, R11), le solde après demande, les chevauchements, les écrasements (R10) et les avertissements (R5).
3. À chaque tour, il affiche la proposition (bornes, type, commentaire, durée décomptée, solde après demande) et ses signalements.
4. L'employé affine la demande en conversant (« plutôt à partir du mercredi ») : c'est le **même** brouillon qui est mis à jour. Il peut aussi ouvrir le brouillon dans le formulaire et son calendrier.
5. **L'agent ne soumet jamais et n'annule jamais.** Pour soumettre, l'employé ouvre le brouillon dans le formulaire, le relit et clique sur « Soumettre ».
6. Au 10ᵉ tour, la conversation est close. Le brouillon reste disponible dans le formulaire, et l'employé peut ouvrir une nouvelle conversation pour une autre demande.

**Exemple** (nous sommes le 01/11/2026) : « je voudrais poser une semaine du 15 au 20 août pour des vacances ».
| Élément | Comportement de l'agent | Signalement |
|---|---|---|
| Année | Il déduit 2027, la prochaine occurrence. | Incertitude |
| 15/08/2027 | C'est un dimanche et un jour férié, donc il est exclu. | Incertitude |
| Période | Du 16/08 matin au 20/08 après-midi, soit 5 j, ce qui est cohérent avec « une semaine ». | — |
| Type | « Vacances » est traduit en CP ; des RTT seraient aussi possibles. | Incertitude |
| Solde | Il vérifie le solde CP. | Incohérence si le solde est insuffisant |
| Forfait jours + « en RTT » | Des JRTT ne peuvent pas être posés sur 2027 (R9). | Incohérence |

### 6.5 Cas limites de l'agent
| # | Situation | Comportement attendu |
|---|---|---|
| **A1** | Hors sujet ou tentative de jailbreak | L'agent renvoie le **message de refus standard** : il n'a pas compris la demande et sert uniquement à préparer une demande de congé. Il ne touche pas au brouillon et ne révèle pas ses consignes. Le tour est décompté. |
| **A2** | Plusieurs congés dans un même message | L'agent prépare le premier, précise qu'il ne traite qu'une demande à la fois et invite l'employé à ouvrir une nouvelle conversation pour les suivants. |
| **A3** | Demande non réalisable : solde insuffisant, date passée (hors maladie), chevauchement, durée nulle, période invalide, JRTT hors de l'année en cours | Le brouillon est enregistré avec une *incohérence* sur le champ concerné. L'agent explique la règle avec les chiffres et prévient que la soumission sera refusée en l'état. Il propose des pistes (raccourcir, changer de type, décaler), **sans les appliquer** tant que l'employé n'a pas donné son accord. |
| **A4** | Information manquante (aucune date exploitable) | L'agent pose une question au lieu de deviner. |
| **A5** | Ambiguïté (« une semaine du 15 au 16 », « jeudi après-midi au lundi », type impossible à déduire) | L'agent fait une hypothèse explicite et la signale comme *incertitude*. |
| **A6** | Motif médical mentionné | L'agent ne le reprend pas dans le brouillon (C2). |
| **A7** | Arrêt maladie qui chevauche des congés déjà posés | L'agent n'y voit pas une incohérence. Il prévient que des congés seront écrasés et donne les jours recrédités (R10). L'écrasement n'a lieu qu'à la soumission manuelle. |
| **A8** | Décompte à temps partiel surprenant (par exemple 2 j pour un mardi) | L'agent explique le décompte légal (R2, 4.1). Ce n'est pas une incohérence. |
| **A9** | Type de congé non géré en V1 (mariage, maternité, enfant malade…) | L'agent répond par ce **message fixe** : « Je ne peux préparer que des congés payés, des RTT, des arrêts maladie ou des congés sans solde. Désolé, ce type de congé n'est pas encore géré par l'application. » Ce type de congé n'est géré ni par l'agent ni par le formulaire. Il ne crée ni ne modifie aucun brouillon. Ce n'est pas le refus standard (A1), car la demande est légitime (D58). |

---

## 7. Conformité RGPD et AI Act

### 7.1 RGPD
| # | Exigence |
|---|---|
| **C1** | **Données de santé** : l'application ne demande jamais de motif médical. Pour la Maladie, aucun champ commentaire n'est proposé (R6). |
| **C2** | **Agent et santé** : si l'employé mentionne un motif médical, l'agent ne le reprend pas et ne pose aucune question sur la santé. Il indique « information médicale non conservée ». |
| **C3** | **Journaux** : les journaux techniques ne contiennent ni commentaire, ni message de conversation, ni motif, ni identité (prénom, nom, poste, manager), ni email. On y trouve seulement des identifiants pseudonymes, des types d'événements et des codes d'erreur ou de signalement. |
| **C4** | **Minimisation vers l'IA** : l'agent ne reçoit que la date du jour et le résultat de simulation de la demande en cours (6.4). Il n'a accès ni à l'identité de l'employé, ni à ses compteurs ou à ses autres demandes, ni à aucun identifiant technique, ni aux autres employés (D55). |
| **C5** | **Cloisonnement** : un employé n'accède qu'à ses propres données. |
| **C6** | **Conservation** : le contenu d'une conversation est purgé quand le brouillon est soumis ou annulé, ou après 30 jours d'abandon. Seules les données structurées sont conservées. |
| **C7** | **Pas de réutilisation** : les échanges ne servent pas à entraîner un modèle. |
| **C8** | **Droits** : l'employé consulte ses données. Il peut annuler ses brouillons et clore ses conversations, dont le contenu est alors purgé ; rien n'est supprimé (C17). L'export et l'effacement complets arrivent en V2. |
| **C15** | **Maladie, type sensible** : aucun commentaire (R6). Dans les journaux, la Maladie apparaît sous un code de catégorie générique (« sensible ») plutôt que sous son type (D34). |
| **C17** | **Traçabilité** : toute action sur une demande (création, modification, soumission, annulation, écrasement) est conservée, avec sa date et son auteur (employé ou agent). Aucune demande n'est supprimée. L'historique ne contient pas le texte des conversations (D56). |

### 7.2 AI Act
| # | Exigence |
|---|---|
| **C9** | **Transparence** (art. 50) : l'employé est informé qu'il échange avec une IA dès l'ouverture de la conversation. Chaque réponse porte la mention « Réponse générée par une IA ». |
| **C10** | **Marquage** : un brouillon venant de l'agent porte la mention « Proposé par l'IA, à vérifier ». Les champs remplis par hypothèse sont mis en évidence. |
| **C11** | **Contrôle humain** : toute action engageante est un geste explicite de l'employé. |
| **C12** | **Classification** : en V1, l'agent aide l'employé à remplir sa propre demande, ce qui correspond à un risque limité. *Point de vigilance pour la V2* : une IA qui recommanderait d'approuver ou de refuser pourrait relever des systèmes RH à haut risque (annexe III). Elle reste donc en dehors de la décision. |
| **C13** | **Traçabilité** : pour chaque demande venant de l'agent, on conserve les champs proposés et les codes de signalement, sans texte libre. |
| **C14** | **Robustesse** : le message de refus standard (A1) et la limite de 10 tours protègent contre les abus. |

---

## 8. Hors périmètre V1
- Authentification, workflow d'approbation, notifications, déploiement.
- Acquisition mensuelle des droits, périodes de référence, conventions collectives plus favorables que le Code du travail.
- Modification d'une demande soumise ; modification de la fiche RH.
- Justificatifs.
- Rythmes de temps partiel autres que le 80 % sur 4 jours (demi-journées, forfait jours réduit).
- Congés familiaux et de parentalité, ainsi que congés de proche aidant, de présence parentale et de solidarité familiale (pistes V2, 9).
- Jours chômés d'usage (par exemple Mardi gras, Mercredi des Cendres ou Mi-Carême aux Antilles) et jours fériés supplémentaires fixés par accord ou convention collective.
- Nouvelle-Calédonie, Polynésie française et Wallis-et-Futuna, qui ont leur propre droit du travail.

---

## 9. Pistes V2
| Thème | Pistes |
|---|---|
| **Workflow d'approbation** | Rattachement de chaque employé à un manager ; validation ou refus motivé ; délégation pendant les absences ; plusieurs niveaux de validation ; approbation automatique de la Maladie ; délai de prévenance. |
| **Notifications** | Événements : soumission, approbation, refus, annulation, relance d'un manager qui n'a pas répondu, déclaration tardive, congés écrasés par un arrêt maladie. Canaux in-app et email, selon les préférences de l'employé. |
| **Scalabilité fonctionnelle** | Plusieurs entités ou pays, avec leurs calendriers fériés ; jours chômés d'usage par établissement (Antilles : Mardi gras, Cendres, Mi-Carême, 21 juillet…) ; pics saisonniers (été, fin d'année) ; soumissions simultanées sur un même compteur ; quotas d'usage de l'agent par employé et par jour ; historique sur plusieurs années. |
| **Droits** | Contrat de travail (modalité, date d'entrée) et calcul des droits à partir du contrat, dont la formule des JRTT (D52) ; acquisition mensuelle (2,08 j de CP par mois) ; période de référence des CP (du 1er juin au 31 mai) ; reports ; autres rythmes de temps partiel (demi-journées, forfait jours réduit) ; accords d'entreprise et conventions collectives plus favorables. |
| **Congés familiaux et de parentalité** | Types : événements familiaux (mariage ou PACS, naissance, décès, annonce d'une pathologie chez un enfant ; art. L3142-4), deuil (art. L3142-1-1), maternité (art. L1225-17 à L1225-19), paternité et accueil de l'enfant (art. L1225-35), adoption (art. L1225-37), congé parental d'éducation (art. L1225-47 et L1225-50), enfant malade (art. L1225-61). Notions à introduire : droit événementiel, décompte en jours calendaires, fenêtre de prise, fractionnement, délai de prévenance, situation déclarée, ancienneté, enfants à charge sur la fiche RH, types sensibles. Pistes associées : employé de démo avec enfants à charge ; justificatifs (actes, certificats) ; partage du congé d'adoption ; report du prénatal vers le postnatal, renonciation et congé pathologique ; ajustement à la date réelle d'accouchement ; congé parental à temps partiel ; congés de proche aidant, de présence parentale et de solidarité familiale ; veille sur les réformes récentes du congé de naissance. |
| **Conformité** | Visibilité de la maladie côté manager (absence seule ?) ; export et effacement RGPD ; registre des traitements ; analyse d'impact si l'IA intervient dans l'approbation. |
| **Autres** | Modification d'une demande soumise ; mise à jour de la fiche RH par les RH ; calendrier d'équipe ; agent côté manager. |

---

## 10. Journal des décisions
Cadrage réalisé le 01/11/2026. Chaque décision renvoie à la demande d'origine, citée telle quelle. Les propositions de l'assistant IA sont signalées comme telles, qu'elles aient été validées par le porteur du projet ou qu'elles restent à valider.

**Décisions V1 encore à valider** : aucune. Les décisions reportées en V2 (D28, D30 à D33, D35, D42, D43) seront à valider avec la V2.

| # | Décision | Source | Sections |
|---|---|---|---|
| D1 | Cadrer la US1 et planifier une V2 (scalabilité, approbation, notifications), en restant sur le fonctionnel | « L'objectif est de développer l'user story 1 mais aussi de planifier les possibles extensions qu'on pourra faire dans une V2 (problèmatique de scalabilité, workflow d'approbation, notifications). L'objectif dans cette discussion est de discuter uniquement fonctionnelle. » | 1, 9 |
| D2 | Pose à la **demi-journée** (Matin / Après-midi) | Choix « Demi-journées », au lieu des journées entières recommandées par l'IA | 3.2, 3.3, R1, R2, R4 |
| D3 | **Brouillons persistés**, avec le statut Brouillon | Choix « Oui, statut Brouillon », au lieu du brouillon éphémère recommandé par l'IA | 3.2, 5, 6.3 |
| D4 | Après soumission, l'employé peut **consulter et annuler** | Choix « Consulter + annuler », qui était la recommandation de l'IA | R7, 5, 6.3 |
| D5 | Agent **conversationnel multi-tours** | Choix « Conversationnel multi-tours », au lieu du mode en un seul échange recommandé par l'IA | 6.4 |
| D6 | RGPD : aucun motif ni donnée personnelle dans les journaux ; minimisation ; cloisonnement | « l'agent et le logiciel de congé doit respecter les règles RGPD ("les commentaires/raisons de l'arret maladie ou donnée personnel salariées ne doivent etre logger) » | 7.1, A6 |
| D7 | AI Act : l'agent s'annonce comme une IA, ses propositions sont marquées, l'humain garde le contrôle | « et l'IA Act (par exemple : typiquement "l'agent doit annoncer que la réponse provient d'une IA") » | 7.2 |
| D8 | Hors sujet ou jailbreak : **message de refus standard** | « lorsque l'utilisateur tente de jailibreack l'agent ou parle de sujet HS : répondre qu'on ne peut pas compris la demande et qu'il est uniquement destiné à ajouter des congés. » | A1, C14 |
| D9 | **Une seule demande** par conversation | « l'agent pourra gérer qu'une seule demande de congé à la fois. » | 3.5, A2 |
| D10 | Une demande non réalisable donne un brouillon signalé, une explication et des pistes, sans application automatique | « cette demande ne peut ne pas aboutir (car solde insuffisant en cas de congés payés par ex, date ultérieure) ou une autre règle métier qui n'est pas respecté » | A3 |
| D11 | **10 tours au plus** par conversation | « je borne à 10 tours de conversations (pour éviter le botting ou des ptits malins jouent avec) » | 3.5, 6.4, C14 |
| D12 | **Pas d'antériorité, sauf pour la Maladie**, avec un délai de déclaration de 48 h | « je confirme le requirement (pas de demande à postériori sauf arret maladie. Normalement l'arret maladie doit etre signalé dans les 48h à l'employeur à partir du début de la maladie. » | R5, 3.3 |
| D13 | Arrêt maladie déclaré après 48 h : **accepté avec un avertissement** | Proposition de l'IA, validée (« oui ») | R5 |
| D14 | Commentaire limité à **200 caractères** | Proposé par le porteur du projet (« 200 caractères te parait suffisant ? ») et confirmé par l'IA : la limite suffit et réduit le risque de données sensibles | R6, 3.2 |
| D15 | Compteurs issus des **données de démo**, conformes au droit français et au contrat | « les soldes sont fixées par données de démo en respectant les règles francaises et les différents type de Contrats 35h-40h / Contrats Jours 218J par An (pour les JRTT) » | R8, 3.1 |
| D16 | **Trois profils seulement** : 35 h (0 RTT), 40 h (10 RTT) et forfait 218 j (JRTT). *Amendée par D36* | « RTT, si 40h 10j si 35h 0j je veux seulement ces 2 profils + profil forfait jour. » | 3.1, R8 |
| D17 | JRTT posables **uniquement sur l'année civile en cours** | « les JRTT sont posables uniquement sur l'année en cours travaillés » | R9, 3.4, A3 |
| D18 | Annulation possible **seulement avant le début** du congé | Proposition de l'IA, validée (« oui ») | R7 |
| D19 | Contenu des conversations conservé **30 jours** au plus après abandon | Proposition de l'IA, validée (« oui ») | C6 |
| D20 | Langage ubiquitaire enrichi | « Améliore d'ailleurs le languages ubiquitaire » | 3 |
| D21 | Tenir un journal des décisions | « trace toutes les décisions qu'on a pris de la conservation avec ce que je t'ai dis » | 10 |
| D22 | Deux canaux de préparation, l'**interface avec un calendrier** et l'**agent**, avec une soumission finale **toujours manuelle** | « la soumission de l'absence par l'utilisateur peut etre fait soit via l'UI : choix des dates via le calendrier soit via l'agent où la soumission finale reste manuelle. » | 6.1, 6.3, 6.4 |
| D23 | Un **arrêt maladie écrase les congés posés** : c'est une exception à l'interdiction de chevauchement | « uen absence d'arret maladie écrase les congés posées (exception à la regle de chevauchement impossible) » | 3.3, R4, R10, A7 |
| D24 | L'écrasement porte sur les **demi-journées communes seulement** : la demande est raccourcie ou scindée, ou passe au statut *Remplacée* si elle est couverte en entier. Les jours retirés sont recrédités, même si le congé a commencé | Proposition de l'IA pour appliquer D23, conforme au droit de reporter des CP interrompus par une maladie, validée : « Valide celle ci » | R10, 3.2, 5 |
| D25 | Ajout des **congés familiaux et de parentalité**. *Reportée en V2 par D51* | « modifie le PRD et ajoute les congés parentales (maternité, paternité), congé pour mariage et autre... » | 9 |
| D26 | Autres types retenus : **enfant malade, congé parental d'éducation, adoption, autres événements familiaux**. *Reportée en V2 par D51* | Choix « Congé enfant malade, Congé parental d'éducation, Congé d'adoption, Autres événements familiaux » | 9 |
| D27 | Nouveaux types **en V1**. *Annulée par D51* | Choix « V1 », qui était la recommandation de l'IA | — |
| D28 | Maternité et paternité en **jours calendaires**. *Reportée en V2 par D51* | Choix « Jours calendaires », qui était la recommandation de l'IA. L'extension à l'adoption et au congé parental est une proposition de l'IA, à valider | 9 |
| D29 | Fenêtre de prise de **± 15 jours** autour de l'événement. *Reportée en V2 par D51* | Choix « ± 15 jours », qui était la recommandation de l'IA | 9 |
| D30 | Événements familiaux décomptés en **jours ouvrés**. *Reportée en V2 par D51* | Proposition de l'IA, à valider | 9 |
| D31 | **Antériorité autorisée pour les événements imprévisibles**, avec un avertissement au-delà de 48 h. *Reportée en V2 par D51* | Proposition de l'IA, à valider | 9 |
| D32 | Délai de prévenance non respecté : **avertissement non bloquant**. *Reportée en V2 par D51* | Proposition de l'IA, à valider | 9 |
| D33 | Un arrêt maladie ne peut pas écraser une maternité, une paternité ou un événement familial. *Reportée en V2 par D51* | Proposition de l'IA, à valider | 9 |
| D34 | **Maladie, type sensible** : pas de commentaire, catégorie générique dans les journaux. Les autres types sensibles sont reportés en V2 par D51 | Proposition de l'IA, validée (« okep ») | R6, C1, C15 |
| D35 | Simplifications : maternité posée pour sa **durée légale complète**, adoption **non partagée**. *Reportée en V2 par D51* | Proposition de l'IA, à valider | 9 |
| D36 | Un **employé à temps partiel** dans la démo (amende D16) | « ajoute aussi un salarié à tremps partiels dans la démo » | 3.1, R8 |
| D37 | Rythme **80 % sur 4 jours**, mercredi non travaillé | Choix « 80 % — 4 jours », qui était la recommandation de l'IA | 3.1, 3.3 |
| D38 | **Décompte légal** des CP à temps partiel | Choix « Décompte légal », qui était la recommandation de l'IA | R2, 4.1, 6.3, A8 |
| D39 | Maladie et Sans solde : **seuls les jours travaillés** comptent | Proposition de l'IA, validée (« okep ») | R2 |
| D40 | Demi-journée avant un jour non travaillé : le jour non travaillé **ne compte pas** (mardi après-midi = 0,5 j). Il ne compte que si le jour travaillé précédent est posé en entier | La proposition de l'IA (1,5 j) a été écartée. Choix « B — 0,5 j », après la question « je comprends pas la D40 » | R2, 4.1 |
| D41 | Un **employé de démo avec deux enfants à charge**, l'un de plus de 1 an et l'autre de moins de 1 an, inscrits sur sa **fiche RH**. *Reportée en V2 par D51* | « dans la démo : ajoute un salarié qui a un enfant à charge de plus d'1 an et un de moins d'un an (cette information doit etre donné dans sa fiche RH si legale) » | 9 |
| D42 | Enfants à charge sur la fiche RH, **réduits à leur date de naissance ou d'arrivée, sans nom** : licite car non sensible au sens de l'art. 9 du RGPD et nécessaire au calcul des droits familiaux. *Reportée en V2 par D51* | Proposition de l'IA pour appliquer D41, à valider | 9 |
| D43 | Plafond Enfant malade selon l'enfant concerné : **5 j au total, dont 3 j au plus** pour les enfants de 1 an ou plus. *Reportée en V2 par D51* | Proposition de l'IA, à valider | 9 |
| D44 | **Jours fériés d'Alsace-Moselle** en V1 : Vendredi saint et 26 décembre (Saint-Étienne) | « ajoute aussi dans le PRD : la gestion de jour férié spécifique à la région (cas Alsace Moselle) : Vendredi Saint et Saint Etienne. » | 3.3, R11 |
| D45 | Calendrier férié déterminé par le **lieu de travail** inscrit sur la fiche RH ; Vendredi saint appliqué aux **trois départements entiers** | Proposition de l'IA pour appliquer D44, validée (« oui ») | 3.1, 3.3, R11, C4 |
| D46 | Employé de démo **`alsace-moselle`** au forfait 218 j, pour montrer l'effet sur les JRTT (10 j en 2027) | Proposition de l'IA, validée (« oui ») | R8, 3.1 |
| D47 | **Calendriers outre-mer** en V1 : commémoration de l'abolition de l'esclavage, à une date propre à chaque territoire. *Annulée par D50* | « ajoute le calendrier de Jour férié outre mer et autres usages locaux » | — |
| D48 | Usages locaux : **jours fériés légaux uniquement** ; les jours chômés d'usage passent en V2 | Choix « Légal uniquement », au lieu de la liste par établissement recommandée par l'IA | 3.3, R11, 8, 9 |
| D49 | Employé de démo **`guadeloupe`**. L'horaire 40 h retenu pour cet employé est un choix de l'IA. *Annulée par D50* | Choix « Oui, en Guadeloupe », qui était la recommandation de l'IA | — |
| D50 | **Retrait des jours fériés d'outre-mer** et de l'employé de démo `guadeloupe` (annule D47 et D49) | « supprime les jours fériés du PRD (ces jours sont pas forcement chome) » | 3.3, R11, R8 |
| D51 | **V1 limitée à quatre types** : CP, RTT, Maladie, Sans solde. Les congés familiaux et de parentalité, les enfants à charge et l'employé de démo associé passent en pistes V2, sans le détail des règles. La Maladie reste un type sensible (D34). Annule D27 | « gardes uniquement ces Types de congé : congés payés, RTT, maladie, sans solde et reportes les autres dans la V2 » ; choix « Simple liste de pistes », « Reporter en V2 » et « Garder C15 » | 1, 3, 4, 6, 7, 8, 9 |
| D52 | **Pas de contrat de travail** : les profils (35 h, 40 h, forfait 218 j, temps partiel 80 %) décrivent seulement les compteurs de démo. Les JRTT sont fixés, pas calculés, et il n'y a pas de date d'entrée. Seuls l'horaire et le lieu de travail restent sur la fiche, pour R2 et R11 | « corrige le PRD avec les ecarts » ; Architecture Brief AD53 : « le contrat n'est pas nécessaire on a juste besoin de ses compteurs » | 3.1, R8, R9 |
| D53 | **Fiche RH limitée à l'identité** (prénom, nom, poste, manager direct), plus l'horaire et le lieu de travail, qui restent internes | « corrige le PRD avec les ecarts » ; Architecture Brief AD44 : « on a juste besoin de fiche rh employé (nom, prénom, poste, manager direct) » | 3.1, C3, C4 |
| D54 | **Tableau de bord** : identité, compteurs (dont jours pris et planifiés) et demandes ; plus de contrat, de date d'entrée ni de lieu de travail | « corrige le PRD avec les ecarts » ; Architecture Brief AD44 et AD53 | 3.4, 6.2 |
| D55 | **Agent sans consultation** : il ne reçoit que la date du jour et le résultat de simulation de la demande en cours | « corrige le PRD avec les ecarts » ; Architecture Brief AD21, tool de lecture « refusé » | 6.4, C4 |
| D56 | **Rien n'est supprimé** : on annule un brouillon, on clôt une conversation (contenu purgé), et toute action est tracée (C17) | « corrige le PRD avec les ecarts » ; Architecture Brief AD42 : « on peut pas delete une requete vu que tout doit etre tracer (outil d'audit » | 3.2, 5, 6.3, C6, C8, C17 |
| D57 | **Code en anglais** : le PRD reste en français ; la correspondance des termes figure dans l'Architecture Brief | « corrige le PRD avec les ecarts » ; Architecture Brief AD22 : « terme métier en anglais, termes techniques en anglais » | 3 |
| D58 | **Type non géré en V1** : message fixe de l'agent, aucun brouillon touché (A9) | « corrige le PRD avec les ecarts » ; Architecture Brief AD39, validé avec le texte : « Je ne peux préparer que des congés payés, des RTT, des arrêts maladie ou des congés sans solde. Désolé, ce type de congé n'est pas encore géré par l'application. » | A9 |
