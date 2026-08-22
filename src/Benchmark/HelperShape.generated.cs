// GENERATED - companion to HelperShapeBenchmarks.cs. Services types implementing IThing<T> for 8,
// 64 and 512 contracts, which is what a generated services type really looks like: one class
// implementing one interface per contract.
//
// Do() reads a field off its ARGUMENT rather than returning a literal, deliberately: a constant
// return folds away entirely once the JIT devirtualises, and the first run of this measured 0.0003
// ns for every arm - which says the benchmark was optimised out, not that the dispatch is free.
#if NET8_0_OR_GREATER
namespace Benchmark
{
    public sealed class Services8 :
        IThing<TypeDispatch.C0>,
        IThing<TypeDispatch.C1>,
        IThing<TypeDispatch.C2>,
        IThing<TypeDispatch.C3>,
        IThing<TypeDispatch.C4>,
        IThing<TypeDispatch.C5>,
        IThing<TypeDispatch.C6>,
        IThing<TypeDispatch.C7>
    {
        int IThing<TypeDispatch.C0>.Do(TypeDispatch.C0 value) => value.Value + 0;
        int IThing<TypeDispatch.C1>.Do(TypeDispatch.C1 value) => value.Value + 1;
        int IThing<TypeDispatch.C2>.Do(TypeDispatch.C2 value) => value.Value + 2;
        int IThing<TypeDispatch.C3>.Do(TypeDispatch.C3 value) => value.Value + 3;
        int IThing<TypeDispatch.C4>.Do(TypeDispatch.C4 value) => value.Value + 4;
        int IThing<TypeDispatch.C5>.Do(TypeDispatch.C5 value) => value.Value + 5;
        int IThing<TypeDispatch.C6>.Do(TypeDispatch.C6 value) => value.Value + 6;
        int IThing<TypeDispatch.C7>.Do(TypeDispatch.C7 value) => value.Value + 7;

        /// <summary>Per-model registration: one assignment per contract, at construction.</summary>
        public void Register()
        {
            ModelOf8.Helper<TypeDispatch.C0>.Instance = this;
            ModelOf8.Helper<TypeDispatch.C1>.Instance = this;
            ModelOf8.Helper<TypeDispatch.C2>.Instance = this;
            ModelOf8.Helper<TypeDispatch.C3>.Instance = this;
            ModelOf8.Helper<TypeDispatch.C4>.Instance = this;
            ModelOf8.Helper<TypeDispatch.C5>.Instance = this;
            ModelOf8.Helper<TypeDispatch.C6>.Instance = this;
            ModelOf8.Helper<TypeDispatch.C7>.Instance = this;
        }
    }

    /// <summary>Stands in for a generated model class; Helper is NESTED, so the slot is per-model.</summary>
    public sealed class ModelOf8
    {
        public static class Helper<T>
        {
            public static IThing<T> Instance;
        }
    }

    public sealed class Services64 :
        IThing<TypeDispatch.C0>,
        IThing<TypeDispatch.C1>,
        IThing<TypeDispatch.C2>,
        IThing<TypeDispatch.C3>,
        IThing<TypeDispatch.C4>,
        IThing<TypeDispatch.C5>,
        IThing<TypeDispatch.C6>,
        IThing<TypeDispatch.C7>,
        IThing<TypeDispatch.C8>,
        IThing<TypeDispatch.C9>,
        IThing<TypeDispatch.C10>,
        IThing<TypeDispatch.C11>,
        IThing<TypeDispatch.C12>,
        IThing<TypeDispatch.C13>,
        IThing<TypeDispatch.C14>,
        IThing<TypeDispatch.C15>,
        IThing<TypeDispatch.C16>,
        IThing<TypeDispatch.C17>,
        IThing<TypeDispatch.C18>,
        IThing<TypeDispatch.C19>,
        IThing<TypeDispatch.C20>,
        IThing<TypeDispatch.C21>,
        IThing<TypeDispatch.C22>,
        IThing<TypeDispatch.C23>,
        IThing<TypeDispatch.C24>,
        IThing<TypeDispatch.C25>,
        IThing<TypeDispatch.C26>,
        IThing<TypeDispatch.C27>,
        IThing<TypeDispatch.C28>,
        IThing<TypeDispatch.C29>,
        IThing<TypeDispatch.C30>,
        IThing<TypeDispatch.C31>,
        IThing<TypeDispatch.C32>,
        IThing<TypeDispatch.C33>,
        IThing<TypeDispatch.C34>,
        IThing<TypeDispatch.C35>,
        IThing<TypeDispatch.C36>,
        IThing<TypeDispatch.C37>,
        IThing<TypeDispatch.C38>,
        IThing<TypeDispatch.C39>,
        IThing<TypeDispatch.C40>,
        IThing<TypeDispatch.C41>,
        IThing<TypeDispatch.C42>,
        IThing<TypeDispatch.C43>,
        IThing<TypeDispatch.C44>,
        IThing<TypeDispatch.C45>,
        IThing<TypeDispatch.C46>,
        IThing<TypeDispatch.C47>,
        IThing<TypeDispatch.C48>,
        IThing<TypeDispatch.C49>,
        IThing<TypeDispatch.C50>,
        IThing<TypeDispatch.C51>,
        IThing<TypeDispatch.C52>,
        IThing<TypeDispatch.C53>,
        IThing<TypeDispatch.C54>,
        IThing<TypeDispatch.C55>,
        IThing<TypeDispatch.C56>,
        IThing<TypeDispatch.C57>,
        IThing<TypeDispatch.C58>,
        IThing<TypeDispatch.C59>,
        IThing<TypeDispatch.C60>,
        IThing<TypeDispatch.C61>,
        IThing<TypeDispatch.C62>,
        IThing<TypeDispatch.C63>
    {
        int IThing<TypeDispatch.C0>.Do(TypeDispatch.C0 value) => value.Value + 0;
        int IThing<TypeDispatch.C1>.Do(TypeDispatch.C1 value) => value.Value + 1;
        int IThing<TypeDispatch.C2>.Do(TypeDispatch.C2 value) => value.Value + 2;
        int IThing<TypeDispatch.C3>.Do(TypeDispatch.C3 value) => value.Value + 3;
        int IThing<TypeDispatch.C4>.Do(TypeDispatch.C4 value) => value.Value + 4;
        int IThing<TypeDispatch.C5>.Do(TypeDispatch.C5 value) => value.Value + 5;
        int IThing<TypeDispatch.C6>.Do(TypeDispatch.C6 value) => value.Value + 6;
        int IThing<TypeDispatch.C7>.Do(TypeDispatch.C7 value) => value.Value + 7;
        int IThing<TypeDispatch.C8>.Do(TypeDispatch.C8 value) => value.Value + 8;
        int IThing<TypeDispatch.C9>.Do(TypeDispatch.C9 value) => value.Value + 9;
        int IThing<TypeDispatch.C10>.Do(TypeDispatch.C10 value) => value.Value + 10;
        int IThing<TypeDispatch.C11>.Do(TypeDispatch.C11 value) => value.Value + 11;
        int IThing<TypeDispatch.C12>.Do(TypeDispatch.C12 value) => value.Value + 12;
        int IThing<TypeDispatch.C13>.Do(TypeDispatch.C13 value) => value.Value + 13;
        int IThing<TypeDispatch.C14>.Do(TypeDispatch.C14 value) => value.Value + 14;
        int IThing<TypeDispatch.C15>.Do(TypeDispatch.C15 value) => value.Value + 15;
        int IThing<TypeDispatch.C16>.Do(TypeDispatch.C16 value) => value.Value + 16;
        int IThing<TypeDispatch.C17>.Do(TypeDispatch.C17 value) => value.Value + 17;
        int IThing<TypeDispatch.C18>.Do(TypeDispatch.C18 value) => value.Value + 18;
        int IThing<TypeDispatch.C19>.Do(TypeDispatch.C19 value) => value.Value + 19;
        int IThing<TypeDispatch.C20>.Do(TypeDispatch.C20 value) => value.Value + 20;
        int IThing<TypeDispatch.C21>.Do(TypeDispatch.C21 value) => value.Value + 21;
        int IThing<TypeDispatch.C22>.Do(TypeDispatch.C22 value) => value.Value + 22;
        int IThing<TypeDispatch.C23>.Do(TypeDispatch.C23 value) => value.Value + 23;
        int IThing<TypeDispatch.C24>.Do(TypeDispatch.C24 value) => value.Value + 24;
        int IThing<TypeDispatch.C25>.Do(TypeDispatch.C25 value) => value.Value + 25;
        int IThing<TypeDispatch.C26>.Do(TypeDispatch.C26 value) => value.Value + 26;
        int IThing<TypeDispatch.C27>.Do(TypeDispatch.C27 value) => value.Value + 27;
        int IThing<TypeDispatch.C28>.Do(TypeDispatch.C28 value) => value.Value + 28;
        int IThing<TypeDispatch.C29>.Do(TypeDispatch.C29 value) => value.Value + 29;
        int IThing<TypeDispatch.C30>.Do(TypeDispatch.C30 value) => value.Value + 30;
        int IThing<TypeDispatch.C31>.Do(TypeDispatch.C31 value) => value.Value + 31;
        int IThing<TypeDispatch.C32>.Do(TypeDispatch.C32 value) => value.Value + 32;
        int IThing<TypeDispatch.C33>.Do(TypeDispatch.C33 value) => value.Value + 33;
        int IThing<TypeDispatch.C34>.Do(TypeDispatch.C34 value) => value.Value + 34;
        int IThing<TypeDispatch.C35>.Do(TypeDispatch.C35 value) => value.Value + 35;
        int IThing<TypeDispatch.C36>.Do(TypeDispatch.C36 value) => value.Value + 36;
        int IThing<TypeDispatch.C37>.Do(TypeDispatch.C37 value) => value.Value + 37;
        int IThing<TypeDispatch.C38>.Do(TypeDispatch.C38 value) => value.Value + 38;
        int IThing<TypeDispatch.C39>.Do(TypeDispatch.C39 value) => value.Value + 39;
        int IThing<TypeDispatch.C40>.Do(TypeDispatch.C40 value) => value.Value + 40;
        int IThing<TypeDispatch.C41>.Do(TypeDispatch.C41 value) => value.Value + 41;
        int IThing<TypeDispatch.C42>.Do(TypeDispatch.C42 value) => value.Value + 42;
        int IThing<TypeDispatch.C43>.Do(TypeDispatch.C43 value) => value.Value + 43;
        int IThing<TypeDispatch.C44>.Do(TypeDispatch.C44 value) => value.Value + 44;
        int IThing<TypeDispatch.C45>.Do(TypeDispatch.C45 value) => value.Value + 45;
        int IThing<TypeDispatch.C46>.Do(TypeDispatch.C46 value) => value.Value + 46;
        int IThing<TypeDispatch.C47>.Do(TypeDispatch.C47 value) => value.Value + 47;
        int IThing<TypeDispatch.C48>.Do(TypeDispatch.C48 value) => value.Value + 48;
        int IThing<TypeDispatch.C49>.Do(TypeDispatch.C49 value) => value.Value + 49;
        int IThing<TypeDispatch.C50>.Do(TypeDispatch.C50 value) => value.Value + 50;
        int IThing<TypeDispatch.C51>.Do(TypeDispatch.C51 value) => value.Value + 51;
        int IThing<TypeDispatch.C52>.Do(TypeDispatch.C52 value) => value.Value + 52;
        int IThing<TypeDispatch.C53>.Do(TypeDispatch.C53 value) => value.Value + 53;
        int IThing<TypeDispatch.C54>.Do(TypeDispatch.C54 value) => value.Value + 54;
        int IThing<TypeDispatch.C55>.Do(TypeDispatch.C55 value) => value.Value + 55;
        int IThing<TypeDispatch.C56>.Do(TypeDispatch.C56 value) => value.Value + 56;
        int IThing<TypeDispatch.C57>.Do(TypeDispatch.C57 value) => value.Value + 57;
        int IThing<TypeDispatch.C58>.Do(TypeDispatch.C58 value) => value.Value + 58;
        int IThing<TypeDispatch.C59>.Do(TypeDispatch.C59 value) => value.Value + 59;
        int IThing<TypeDispatch.C60>.Do(TypeDispatch.C60 value) => value.Value + 60;
        int IThing<TypeDispatch.C61>.Do(TypeDispatch.C61 value) => value.Value + 61;
        int IThing<TypeDispatch.C62>.Do(TypeDispatch.C62 value) => value.Value + 62;
        int IThing<TypeDispatch.C63>.Do(TypeDispatch.C63 value) => value.Value + 63;

        /// <summary>Per-model registration: one assignment per contract, at construction.</summary>
        public void Register()
        {
            ModelOf64.Helper<TypeDispatch.C0>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C1>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C2>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C3>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C4>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C5>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C6>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C7>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C8>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C9>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C10>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C11>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C12>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C13>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C14>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C15>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C16>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C17>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C18>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C19>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C20>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C21>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C22>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C23>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C24>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C25>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C26>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C27>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C28>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C29>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C30>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C31>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C32>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C33>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C34>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C35>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C36>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C37>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C38>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C39>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C40>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C41>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C42>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C43>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C44>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C45>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C46>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C47>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C48>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C49>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C50>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C51>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C52>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C53>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C54>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C55>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C56>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C57>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C58>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C59>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C60>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C61>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C62>.Instance = this;
            ModelOf64.Helper<TypeDispatch.C63>.Instance = this;
        }
    }

    /// <summary>Stands in for a generated model class; Helper is NESTED, so the slot is per-model.</summary>
    public sealed class ModelOf64
    {
        public static class Helper<T>
        {
            public static IThing<T> Instance;
        }
    }

    public sealed class Services512 :
        IThing<TypeDispatch.C0>,
        IThing<TypeDispatch.C1>,
        IThing<TypeDispatch.C2>,
        IThing<TypeDispatch.C3>,
        IThing<TypeDispatch.C4>,
        IThing<TypeDispatch.C5>,
        IThing<TypeDispatch.C6>,
        IThing<TypeDispatch.C7>,
        IThing<TypeDispatch.C8>,
        IThing<TypeDispatch.C9>,
        IThing<TypeDispatch.C10>,
        IThing<TypeDispatch.C11>,
        IThing<TypeDispatch.C12>,
        IThing<TypeDispatch.C13>,
        IThing<TypeDispatch.C14>,
        IThing<TypeDispatch.C15>,
        IThing<TypeDispatch.C16>,
        IThing<TypeDispatch.C17>,
        IThing<TypeDispatch.C18>,
        IThing<TypeDispatch.C19>,
        IThing<TypeDispatch.C20>,
        IThing<TypeDispatch.C21>,
        IThing<TypeDispatch.C22>,
        IThing<TypeDispatch.C23>,
        IThing<TypeDispatch.C24>,
        IThing<TypeDispatch.C25>,
        IThing<TypeDispatch.C26>,
        IThing<TypeDispatch.C27>,
        IThing<TypeDispatch.C28>,
        IThing<TypeDispatch.C29>,
        IThing<TypeDispatch.C30>,
        IThing<TypeDispatch.C31>,
        IThing<TypeDispatch.C32>,
        IThing<TypeDispatch.C33>,
        IThing<TypeDispatch.C34>,
        IThing<TypeDispatch.C35>,
        IThing<TypeDispatch.C36>,
        IThing<TypeDispatch.C37>,
        IThing<TypeDispatch.C38>,
        IThing<TypeDispatch.C39>,
        IThing<TypeDispatch.C40>,
        IThing<TypeDispatch.C41>,
        IThing<TypeDispatch.C42>,
        IThing<TypeDispatch.C43>,
        IThing<TypeDispatch.C44>,
        IThing<TypeDispatch.C45>,
        IThing<TypeDispatch.C46>,
        IThing<TypeDispatch.C47>,
        IThing<TypeDispatch.C48>,
        IThing<TypeDispatch.C49>,
        IThing<TypeDispatch.C50>,
        IThing<TypeDispatch.C51>,
        IThing<TypeDispatch.C52>,
        IThing<TypeDispatch.C53>,
        IThing<TypeDispatch.C54>,
        IThing<TypeDispatch.C55>,
        IThing<TypeDispatch.C56>,
        IThing<TypeDispatch.C57>,
        IThing<TypeDispatch.C58>,
        IThing<TypeDispatch.C59>,
        IThing<TypeDispatch.C60>,
        IThing<TypeDispatch.C61>,
        IThing<TypeDispatch.C62>,
        IThing<TypeDispatch.C63>,
        IThing<TypeDispatch.C64>,
        IThing<TypeDispatch.C65>,
        IThing<TypeDispatch.C66>,
        IThing<TypeDispatch.C67>,
        IThing<TypeDispatch.C68>,
        IThing<TypeDispatch.C69>,
        IThing<TypeDispatch.C70>,
        IThing<TypeDispatch.C71>,
        IThing<TypeDispatch.C72>,
        IThing<TypeDispatch.C73>,
        IThing<TypeDispatch.C74>,
        IThing<TypeDispatch.C75>,
        IThing<TypeDispatch.C76>,
        IThing<TypeDispatch.C77>,
        IThing<TypeDispatch.C78>,
        IThing<TypeDispatch.C79>,
        IThing<TypeDispatch.C80>,
        IThing<TypeDispatch.C81>,
        IThing<TypeDispatch.C82>,
        IThing<TypeDispatch.C83>,
        IThing<TypeDispatch.C84>,
        IThing<TypeDispatch.C85>,
        IThing<TypeDispatch.C86>,
        IThing<TypeDispatch.C87>,
        IThing<TypeDispatch.C88>,
        IThing<TypeDispatch.C89>,
        IThing<TypeDispatch.C90>,
        IThing<TypeDispatch.C91>,
        IThing<TypeDispatch.C92>,
        IThing<TypeDispatch.C93>,
        IThing<TypeDispatch.C94>,
        IThing<TypeDispatch.C95>,
        IThing<TypeDispatch.C96>,
        IThing<TypeDispatch.C97>,
        IThing<TypeDispatch.C98>,
        IThing<TypeDispatch.C99>,
        IThing<TypeDispatch.C100>,
        IThing<TypeDispatch.C101>,
        IThing<TypeDispatch.C102>,
        IThing<TypeDispatch.C103>,
        IThing<TypeDispatch.C104>,
        IThing<TypeDispatch.C105>,
        IThing<TypeDispatch.C106>,
        IThing<TypeDispatch.C107>,
        IThing<TypeDispatch.C108>,
        IThing<TypeDispatch.C109>,
        IThing<TypeDispatch.C110>,
        IThing<TypeDispatch.C111>,
        IThing<TypeDispatch.C112>,
        IThing<TypeDispatch.C113>,
        IThing<TypeDispatch.C114>,
        IThing<TypeDispatch.C115>,
        IThing<TypeDispatch.C116>,
        IThing<TypeDispatch.C117>,
        IThing<TypeDispatch.C118>,
        IThing<TypeDispatch.C119>,
        IThing<TypeDispatch.C120>,
        IThing<TypeDispatch.C121>,
        IThing<TypeDispatch.C122>,
        IThing<TypeDispatch.C123>,
        IThing<TypeDispatch.C124>,
        IThing<TypeDispatch.C125>,
        IThing<TypeDispatch.C126>,
        IThing<TypeDispatch.C127>,
        IThing<TypeDispatch.C128>,
        IThing<TypeDispatch.C129>,
        IThing<TypeDispatch.C130>,
        IThing<TypeDispatch.C131>,
        IThing<TypeDispatch.C132>,
        IThing<TypeDispatch.C133>,
        IThing<TypeDispatch.C134>,
        IThing<TypeDispatch.C135>,
        IThing<TypeDispatch.C136>,
        IThing<TypeDispatch.C137>,
        IThing<TypeDispatch.C138>,
        IThing<TypeDispatch.C139>,
        IThing<TypeDispatch.C140>,
        IThing<TypeDispatch.C141>,
        IThing<TypeDispatch.C142>,
        IThing<TypeDispatch.C143>,
        IThing<TypeDispatch.C144>,
        IThing<TypeDispatch.C145>,
        IThing<TypeDispatch.C146>,
        IThing<TypeDispatch.C147>,
        IThing<TypeDispatch.C148>,
        IThing<TypeDispatch.C149>,
        IThing<TypeDispatch.C150>,
        IThing<TypeDispatch.C151>,
        IThing<TypeDispatch.C152>,
        IThing<TypeDispatch.C153>,
        IThing<TypeDispatch.C154>,
        IThing<TypeDispatch.C155>,
        IThing<TypeDispatch.C156>,
        IThing<TypeDispatch.C157>,
        IThing<TypeDispatch.C158>,
        IThing<TypeDispatch.C159>,
        IThing<TypeDispatch.C160>,
        IThing<TypeDispatch.C161>,
        IThing<TypeDispatch.C162>,
        IThing<TypeDispatch.C163>,
        IThing<TypeDispatch.C164>,
        IThing<TypeDispatch.C165>,
        IThing<TypeDispatch.C166>,
        IThing<TypeDispatch.C167>,
        IThing<TypeDispatch.C168>,
        IThing<TypeDispatch.C169>,
        IThing<TypeDispatch.C170>,
        IThing<TypeDispatch.C171>,
        IThing<TypeDispatch.C172>,
        IThing<TypeDispatch.C173>,
        IThing<TypeDispatch.C174>,
        IThing<TypeDispatch.C175>,
        IThing<TypeDispatch.C176>,
        IThing<TypeDispatch.C177>,
        IThing<TypeDispatch.C178>,
        IThing<TypeDispatch.C179>,
        IThing<TypeDispatch.C180>,
        IThing<TypeDispatch.C181>,
        IThing<TypeDispatch.C182>,
        IThing<TypeDispatch.C183>,
        IThing<TypeDispatch.C184>,
        IThing<TypeDispatch.C185>,
        IThing<TypeDispatch.C186>,
        IThing<TypeDispatch.C187>,
        IThing<TypeDispatch.C188>,
        IThing<TypeDispatch.C189>,
        IThing<TypeDispatch.C190>,
        IThing<TypeDispatch.C191>,
        IThing<TypeDispatch.C192>,
        IThing<TypeDispatch.C193>,
        IThing<TypeDispatch.C194>,
        IThing<TypeDispatch.C195>,
        IThing<TypeDispatch.C196>,
        IThing<TypeDispatch.C197>,
        IThing<TypeDispatch.C198>,
        IThing<TypeDispatch.C199>,
        IThing<TypeDispatch.C200>,
        IThing<TypeDispatch.C201>,
        IThing<TypeDispatch.C202>,
        IThing<TypeDispatch.C203>,
        IThing<TypeDispatch.C204>,
        IThing<TypeDispatch.C205>,
        IThing<TypeDispatch.C206>,
        IThing<TypeDispatch.C207>,
        IThing<TypeDispatch.C208>,
        IThing<TypeDispatch.C209>,
        IThing<TypeDispatch.C210>,
        IThing<TypeDispatch.C211>,
        IThing<TypeDispatch.C212>,
        IThing<TypeDispatch.C213>,
        IThing<TypeDispatch.C214>,
        IThing<TypeDispatch.C215>,
        IThing<TypeDispatch.C216>,
        IThing<TypeDispatch.C217>,
        IThing<TypeDispatch.C218>,
        IThing<TypeDispatch.C219>,
        IThing<TypeDispatch.C220>,
        IThing<TypeDispatch.C221>,
        IThing<TypeDispatch.C222>,
        IThing<TypeDispatch.C223>,
        IThing<TypeDispatch.C224>,
        IThing<TypeDispatch.C225>,
        IThing<TypeDispatch.C226>,
        IThing<TypeDispatch.C227>,
        IThing<TypeDispatch.C228>,
        IThing<TypeDispatch.C229>,
        IThing<TypeDispatch.C230>,
        IThing<TypeDispatch.C231>,
        IThing<TypeDispatch.C232>,
        IThing<TypeDispatch.C233>,
        IThing<TypeDispatch.C234>,
        IThing<TypeDispatch.C235>,
        IThing<TypeDispatch.C236>,
        IThing<TypeDispatch.C237>,
        IThing<TypeDispatch.C238>,
        IThing<TypeDispatch.C239>,
        IThing<TypeDispatch.C240>,
        IThing<TypeDispatch.C241>,
        IThing<TypeDispatch.C242>,
        IThing<TypeDispatch.C243>,
        IThing<TypeDispatch.C244>,
        IThing<TypeDispatch.C245>,
        IThing<TypeDispatch.C246>,
        IThing<TypeDispatch.C247>,
        IThing<TypeDispatch.C248>,
        IThing<TypeDispatch.C249>,
        IThing<TypeDispatch.C250>,
        IThing<TypeDispatch.C251>,
        IThing<TypeDispatch.C252>,
        IThing<TypeDispatch.C253>,
        IThing<TypeDispatch.C254>,
        IThing<TypeDispatch.C255>,
        IThing<TypeDispatch.C256>,
        IThing<TypeDispatch.C257>,
        IThing<TypeDispatch.C258>,
        IThing<TypeDispatch.C259>,
        IThing<TypeDispatch.C260>,
        IThing<TypeDispatch.C261>,
        IThing<TypeDispatch.C262>,
        IThing<TypeDispatch.C263>,
        IThing<TypeDispatch.C264>,
        IThing<TypeDispatch.C265>,
        IThing<TypeDispatch.C266>,
        IThing<TypeDispatch.C267>,
        IThing<TypeDispatch.C268>,
        IThing<TypeDispatch.C269>,
        IThing<TypeDispatch.C270>,
        IThing<TypeDispatch.C271>,
        IThing<TypeDispatch.C272>,
        IThing<TypeDispatch.C273>,
        IThing<TypeDispatch.C274>,
        IThing<TypeDispatch.C275>,
        IThing<TypeDispatch.C276>,
        IThing<TypeDispatch.C277>,
        IThing<TypeDispatch.C278>,
        IThing<TypeDispatch.C279>,
        IThing<TypeDispatch.C280>,
        IThing<TypeDispatch.C281>,
        IThing<TypeDispatch.C282>,
        IThing<TypeDispatch.C283>,
        IThing<TypeDispatch.C284>,
        IThing<TypeDispatch.C285>,
        IThing<TypeDispatch.C286>,
        IThing<TypeDispatch.C287>,
        IThing<TypeDispatch.C288>,
        IThing<TypeDispatch.C289>,
        IThing<TypeDispatch.C290>,
        IThing<TypeDispatch.C291>,
        IThing<TypeDispatch.C292>,
        IThing<TypeDispatch.C293>,
        IThing<TypeDispatch.C294>,
        IThing<TypeDispatch.C295>,
        IThing<TypeDispatch.C296>,
        IThing<TypeDispatch.C297>,
        IThing<TypeDispatch.C298>,
        IThing<TypeDispatch.C299>,
        IThing<TypeDispatch.C300>,
        IThing<TypeDispatch.C301>,
        IThing<TypeDispatch.C302>,
        IThing<TypeDispatch.C303>,
        IThing<TypeDispatch.C304>,
        IThing<TypeDispatch.C305>,
        IThing<TypeDispatch.C306>,
        IThing<TypeDispatch.C307>,
        IThing<TypeDispatch.C308>,
        IThing<TypeDispatch.C309>,
        IThing<TypeDispatch.C310>,
        IThing<TypeDispatch.C311>,
        IThing<TypeDispatch.C312>,
        IThing<TypeDispatch.C313>,
        IThing<TypeDispatch.C314>,
        IThing<TypeDispatch.C315>,
        IThing<TypeDispatch.C316>,
        IThing<TypeDispatch.C317>,
        IThing<TypeDispatch.C318>,
        IThing<TypeDispatch.C319>,
        IThing<TypeDispatch.C320>,
        IThing<TypeDispatch.C321>,
        IThing<TypeDispatch.C322>,
        IThing<TypeDispatch.C323>,
        IThing<TypeDispatch.C324>,
        IThing<TypeDispatch.C325>,
        IThing<TypeDispatch.C326>,
        IThing<TypeDispatch.C327>,
        IThing<TypeDispatch.C328>,
        IThing<TypeDispatch.C329>,
        IThing<TypeDispatch.C330>,
        IThing<TypeDispatch.C331>,
        IThing<TypeDispatch.C332>,
        IThing<TypeDispatch.C333>,
        IThing<TypeDispatch.C334>,
        IThing<TypeDispatch.C335>,
        IThing<TypeDispatch.C336>,
        IThing<TypeDispatch.C337>,
        IThing<TypeDispatch.C338>,
        IThing<TypeDispatch.C339>,
        IThing<TypeDispatch.C340>,
        IThing<TypeDispatch.C341>,
        IThing<TypeDispatch.C342>,
        IThing<TypeDispatch.C343>,
        IThing<TypeDispatch.C344>,
        IThing<TypeDispatch.C345>,
        IThing<TypeDispatch.C346>,
        IThing<TypeDispatch.C347>,
        IThing<TypeDispatch.C348>,
        IThing<TypeDispatch.C349>,
        IThing<TypeDispatch.C350>,
        IThing<TypeDispatch.C351>,
        IThing<TypeDispatch.C352>,
        IThing<TypeDispatch.C353>,
        IThing<TypeDispatch.C354>,
        IThing<TypeDispatch.C355>,
        IThing<TypeDispatch.C356>,
        IThing<TypeDispatch.C357>,
        IThing<TypeDispatch.C358>,
        IThing<TypeDispatch.C359>,
        IThing<TypeDispatch.C360>,
        IThing<TypeDispatch.C361>,
        IThing<TypeDispatch.C362>,
        IThing<TypeDispatch.C363>,
        IThing<TypeDispatch.C364>,
        IThing<TypeDispatch.C365>,
        IThing<TypeDispatch.C366>,
        IThing<TypeDispatch.C367>,
        IThing<TypeDispatch.C368>,
        IThing<TypeDispatch.C369>,
        IThing<TypeDispatch.C370>,
        IThing<TypeDispatch.C371>,
        IThing<TypeDispatch.C372>,
        IThing<TypeDispatch.C373>,
        IThing<TypeDispatch.C374>,
        IThing<TypeDispatch.C375>,
        IThing<TypeDispatch.C376>,
        IThing<TypeDispatch.C377>,
        IThing<TypeDispatch.C378>,
        IThing<TypeDispatch.C379>,
        IThing<TypeDispatch.C380>,
        IThing<TypeDispatch.C381>,
        IThing<TypeDispatch.C382>,
        IThing<TypeDispatch.C383>,
        IThing<TypeDispatch.C384>,
        IThing<TypeDispatch.C385>,
        IThing<TypeDispatch.C386>,
        IThing<TypeDispatch.C387>,
        IThing<TypeDispatch.C388>,
        IThing<TypeDispatch.C389>,
        IThing<TypeDispatch.C390>,
        IThing<TypeDispatch.C391>,
        IThing<TypeDispatch.C392>,
        IThing<TypeDispatch.C393>,
        IThing<TypeDispatch.C394>,
        IThing<TypeDispatch.C395>,
        IThing<TypeDispatch.C396>,
        IThing<TypeDispatch.C397>,
        IThing<TypeDispatch.C398>,
        IThing<TypeDispatch.C399>,
        IThing<TypeDispatch.C400>,
        IThing<TypeDispatch.C401>,
        IThing<TypeDispatch.C402>,
        IThing<TypeDispatch.C403>,
        IThing<TypeDispatch.C404>,
        IThing<TypeDispatch.C405>,
        IThing<TypeDispatch.C406>,
        IThing<TypeDispatch.C407>,
        IThing<TypeDispatch.C408>,
        IThing<TypeDispatch.C409>,
        IThing<TypeDispatch.C410>,
        IThing<TypeDispatch.C411>,
        IThing<TypeDispatch.C412>,
        IThing<TypeDispatch.C413>,
        IThing<TypeDispatch.C414>,
        IThing<TypeDispatch.C415>,
        IThing<TypeDispatch.C416>,
        IThing<TypeDispatch.C417>,
        IThing<TypeDispatch.C418>,
        IThing<TypeDispatch.C419>,
        IThing<TypeDispatch.C420>,
        IThing<TypeDispatch.C421>,
        IThing<TypeDispatch.C422>,
        IThing<TypeDispatch.C423>,
        IThing<TypeDispatch.C424>,
        IThing<TypeDispatch.C425>,
        IThing<TypeDispatch.C426>,
        IThing<TypeDispatch.C427>,
        IThing<TypeDispatch.C428>,
        IThing<TypeDispatch.C429>,
        IThing<TypeDispatch.C430>,
        IThing<TypeDispatch.C431>,
        IThing<TypeDispatch.C432>,
        IThing<TypeDispatch.C433>,
        IThing<TypeDispatch.C434>,
        IThing<TypeDispatch.C435>,
        IThing<TypeDispatch.C436>,
        IThing<TypeDispatch.C437>,
        IThing<TypeDispatch.C438>,
        IThing<TypeDispatch.C439>,
        IThing<TypeDispatch.C440>,
        IThing<TypeDispatch.C441>,
        IThing<TypeDispatch.C442>,
        IThing<TypeDispatch.C443>,
        IThing<TypeDispatch.C444>,
        IThing<TypeDispatch.C445>,
        IThing<TypeDispatch.C446>,
        IThing<TypeDispatch.C447>,
        IThing<TypeDispatch.C448>,
        IThing<TypeDispatch.C449>,
        IThing<TypeDispatch.C450>,
        IThing<TypeDispatch.C451>,
        IThing<TypeDispatch.C452>,
        IThing<TypeDispatch.C453>,
        IThing<TypeDispatch.C454>,
        IThing<TypeDispatch.C455>,
        IThing<TypeDispatch.C456>,
        IThing<TypeDispatch.C457>,
        IThing<TypeDispatch.C458>,
        IThing<TypeDispatch.C459>,
        IThing<TypeDispatch.C460>,
        IThing<TypeDispatch.C461>,
        IThing<TypeDispatch.C462>,
        IThing<TypeDispatch.C463>,
        IThing<TypeDispatch.C464>,
        IThing<TypeDispatch.C465>,
        IThing<TypeDispatch.C466>,
        IThing<TypeDispatch.C467>,
        IThing<TypeDispatch.C468>,
        IThing<TypeDispatch.C469>,
        IThing<TypeDispatch.C470>,
        IThing<TypeDispatch.C471>,
        IThing<TypeDispatch.C472>,
        IThing<TypeDispatch.C473>,
        IThing<TypeDispatch.C474>,
        IThing<TypeDispatch.C475>,
        IThing<TypeDispatch.C476>,
        IThing<TypeDispatch.C477>,
        IThing<TypeDispatch.C478>,
        IThing<TypeDispatch.C479>,
        IThing<TypeDispatch.C480>,
        IThing<TypeDispatch.C481>,
        IThing<TypeDispatch.C482>,
        IThing<TypeDispatch.C483>,
        IThing<TypeDispatch.C484>,
        IThing<TypeDispatch.C485>,
        IThing<TypeDispatch.C486>,
        IThing<TypeDispatch.C487>,
        IThing<TypeDispatch.C488>,
        IThing<TypeDispatch.C489>,
        IThing<TypeDispatch.C490>,
        IThing<TypeDispatch.C491>,
        IThing<TypeDispatch.C492>,
        IThing<TypeDispatch.C493>,
        IThing<TypeDispatch.C494>,
        IThing<TypeDispatch.C495>,
        IThing<TypeDispatch.C496>,
        IThing<TypeDispatch.C497>,
        IThing<TypeDispatch.C498>,
        IThing<TypeDispatch.C499>,
        IThing<TypeDispatch.C500>,
        IThing<TypeDispatch.C501>,
        IThing<TypeDispatch.C502>,
        IThing<TypeDispatch.C503>,
        IThing<TypeDispatch.C504>,
        IThing<TypeDispatch.C505>,
        IThing<TypeDispatch.C506>,
        IThing<TypeDispatch.C507>,
        IThing<TypeDispatch.C508>,
        IThing<TypeDispatch.C509>,
        IThing<TypeDispatch.C510>,
        IThing<TypeDispatch.C511>
    {
        int IThing<TypeDispatch.C0>.Do(TypeDispatch.C0 value) => value.Value + 0;
        int IThing<TypeDispatch.C1>.Do(TypeDispatch.C1 value) => value.Value + 1;
        int IThing<TypeDispatch.C2>.Do(TypeDispatch.C2 value) => value.Value + 2;
        int IThing<TypeDispatch.C3>.Do(TypeDispatch.C3 value) => value.Value + 3;
        int IThing<TypeDispatch.C4>.Do(TypeDispatch.C4 value) => value.Value + 4;
        int IThing<TypeDispatch.C5>.Do(TypeDispatch.C5 value) => value.Value + 5;
        int IThing<TypeDispatch.C6>.Do(TypeDispatch.C6 value) => value.Value + 6;
        int IThing<TypeDispatch.C7>.Do(TypeDispatch.C7 value) => value.Value + 7;
        int IThing<TypeDispatch.C8>.Do(TypeDispatch.C8 value) => value.Value + 8;
        int IThing<TypeDispatch.C9>.Do(TypeDispatch.C9 value) => value.Value + 9;
        int IThing<TypeDispatch.C10>.Do(TypeDispatch.C10 value) => value.Value + 10;
        int IThing<TypeDispatch.C11>.Do(TypeDispatch.C11 value) => value.Value + 11;
        int IThing<TypeDispatch.C12>.Do(TypeDispatch.C12 value) => value.Value + 12;
        int IThing<TypeDispatch.C13>.Do(TypeDispatch.C13 value) => value.Value + 13;
        int IThing<TypeDispatch.C14>.Do(TypeDispatch.C14 value) => value.Value + 14;
        int IThing<TypeDispatch.C15>.Do(TypeDispatch.C15 value) => value.Value + 15;
        int IThing<TypeDispatch.C16>.Do(TypeDispatch.C16 value) => value.Value + 16;
        int IThing<TypeDispatch.C17>.Do(TypeDispatch.C17 value) => value.Value + 17;
        int IThing<TypeDispatch.C18>.Do(TypeDispatch.C18 value) => value.Value + 18;
        int IThing<TypeDispatch.C19>.Do(TypeDispatch.C19 value) => value.Value + 19;
        int IThing<TypeDispatch.C20>.Do(TypeDispatch.C20 value) => value.Value + 20;
        int IThing<TypeDispatch.C21>.Do(TypeDispatch.C21 value) => value.Value + 21;
        int IThing<TypeDispatch.C22>.Do(TypeDispatch.C22 value) => value.Value + 22;
        int IThing<TypeDispatch.C23>.Do(TypeDispatch.C23 value) => value.Value + 23;
        int IThing<TypeDispatch.C24>.Do(TypeDispatch.C24 value) => value.Value + 24;
        int IThing<TypeDispatch.C25>.Do(TypeDispatch.C25 value) => value.Value + 25;
        int IThing<TypeDispatch.C26>.Do(TypeDispatch.C26 value) => value.Value + 26;
        int IThing<TypeDispatch.C27>.Do(TypeDispatch.C27 value) => value.Value + 27;
        int IThing<TypeDispatch.C28>.Do(TypeDispatch.C28 value) => value.Value + 28;
        int IThing<TypeDispatch.C29>.Do(TypeDispatch.C29 value) => value.Value + 29;
        int IThing<TypeDispatch.C30>.Do(TypeDispatch.C30 value) => value.Value + 30;
        int IThing<TypeDispatch.C31>.Do(TypeDispatch.C31 value) => value.Value + 31;
        int IThing<TypeDispatch.C32>.Do(TypeDispatch.C32 value) => value.Value + 32;
        int IThing<TypeDispatch.C33>.Do(TypeDispatch.C33 value) => value.Value + 33;
        int IThing<TypeDispatch.C34>.Do(TypeDispatch.C34 value) => value.Value + 34;
        int IThing<TypeDispatch.C35>.Do(TypeDispatch.C35 value) => value.Value + 35;
        int IThing<TypeDispatch.C36>.Do(TypeDispatch.C36 value) => value.Value + 36;
        int IThing<TypeDispatch.C37>.Do(TypeDispatch.C37 value) => value.Value + 37;
        int IThing<TypeDispatch.C38>.Do(TypeDispatch.C38 value) => value.Value + 38;
        int IThing<TypeDispatch.C39>.Do(TypeDispatch.C39 value) => value.Value + 39;
        int IThing<TypeDispatch.C40>.Do(TypeDispatch.C40 value) => value.Value + 40;
        int IThing<TypeDispatch.C41>.Do(TypeDispatch.C41 value) => value.Value + 41;
        int IThing<TypeDispatch.C42>.Do(TypeDispatch.C42 value) => value.Value + 42;
        int IThing<TypeDispatch.C43>.Do(TypeDispatch.C43 value) => value.Value + 43;
        int IThing<TypeDispatch.C44>.Do(TypeDispatch.C44 value) => value.Value + 44;
        int IThing<TypeDispatch.C45>.Do(TypeDispatch.C45 value) => value.Value + 45;
        int IThing<TypeDispatch.C46>.Do(TypeDispatch.C46 value) => value.Value + 46;
        int IThing<TypeDispatch.C47>.Do(TypeDispatch.C47 value) => value.Value + 47;
        int IThing<TypeDispatch.C48>.Do(TypeDispatch.C48 value) => value.Value + 48;
        int IThing<TypeDispatch.C49>.Do(TypeDispatch.C49 value) => value.Value + 49;
        int IThing<TypeDispatch.C50>.Do(TypeDispatch.C50 value) => value.Value + 50;
        int IThing<TypeDispatch.C51>.Do(TypeDispatch.C51 value) => value.Value + 51;
        int IThing<TypeDispatch.C52>.Do(TypeDispatch.C52 value) => value.Value + 52;
        int IThing<TypeDispatch.C53>.Do(TypeDispatch.C53 value) => value.Value + 53;
        int IThing<TypeDispatch.C54>.Do(TypeDispatch.C54 value) => value.Value + 54;
        int IThing<TypeDispatch.C55>.Do(TypeDispatch.C55 value) => value.Value + 55;
        int IThing<TypeDispatch.C56>.Do(TypeDispatch.C56 value) => value.Value + 56;
        int IThing<TypeDispatch.C57>.Do(TypeDispatch.C57 value) => value.Value + 57;
        int IThing<TypeDispatch.C58>.Do(TypeDispatch.C58 value) => value.Value + 58;
        int IThing<TypeDispatch.C59>.Do(TypeDispatch.C59 value) => value.Value + 59;
        int IThing<TypeDispatch.C60>.Do(TypeDispatch.C60 value) => value.Value + 60;
        int IThing<TypeDispatch.C61>.Do(TypeDispatch.C61 value) => value.Value + 61;
        int IThing<TypeDispatch.C62>.Do(TypeDispatch.C62 value) => value.Value + 62;
        int IThing<TypeDispatch.C63>.Do(TypeDispatch.C63 value) => value.Value + 63;
        int IThing<TypeDispatch.C64>.Do(TypeDispatch.C64 value) => value.Value + 64;
        int IThing<TypeDispatch.C65>.Do(TypeDispatch.C65 value) => value.Value + 65;
        int IThing<TypeDispatch.C66>.Do(TypeDispatch.C66 value) => value.Value + 66;
        int IThing<TypeDispatch.C67>.Do(TypeDispatch.C67 value) => value.Value + 67;
        int IThing<TypeDispatch.C68>.Do(TypeDispatch.C68 value) => value.Value + 68;
        int IThing<TypeDispatch.C69>.Do(TypeDispatch.C69 value) => value.Value + 69;
        int IThing<TypeDispatch.C70>.Do(TypeDispatch.C70 value) => value.Value + 70;
        int IThing<TypeDispatch.C71>.Do(TypeDispatch.C71 value) => value.Value + 71;
        int IThing<TypeDispatch.C72>.Do(TypeDispatch.C72 value) => value.Value + 72;
        int IThing<TypeDispatch.C73>.Do(TypeDispatch.C73 value) => value.Value + 73;
        int IThing<TypeDispatch.C74>.Do(TypeDispatch.C74 value) => value.Value + 74;
        int IThing<TypeDispatch.C75>.Do(TypeDispatch.C75 value) => value.Value + 75;
        int IThing<TypeDispatch.C76>.Do(TypeDispatch.C76 value) => value.Value + 76;
        int IThing<TypeDispatch.C77>.Do(TypeDispatch.C77 value) => value.Value + 77;
        int IThing<TypeDispatch.C78>.Do(TypeDispatch.C78 value) => value.Value + 78;
        int IThing<TypeDispatch.C79>.Do(TypeDispatch.C79 value) => value.Value + 79;
        int IThing<TypeDispatch.C80>.Do(TypeDispatch.C80 value) => value.Value + 80;
        int IThing<TypeDispatch.C81>.Do(TypeDispatch.C81 value) => value.Value + 81;
        int IThing<TypeDispatch.C82>.Do(TypeDispatch.C82 value) => value.Value + 82;
        int IThing<TypeDispatch.C83>.Do(TypeDispatch.C83 value) => value.Value + 83;
        int IThing<TypeDispatch.C84>.Do(TypeDispatch.C84 value) => value.Value + 84;
        int IThing<TypeDispatch.C85>.Do(TypeDispatch.C85 value) => value.Value + 85;
        int IThing<TypeDispatch.C86>.Do(TypeDispatch.C86 value) => value.Value + 86;
        int IThing<TypeDispatch.C87>.Do(TypeDispatch.C87 value) => value.Value + 87;
        int IThing<TypeDispatch.C88>.Do(TypeDispatch.C88 value) => value.Value + 88;
        int IThing<TypeDispatch.C89>.Do(TypeDispatch.C89 value) => value.Value + 89;
        int IThing<TypeDispatch.C90>.Do(TypeDispatch.C90 value) => value.Value + 90;
        int IThing<TypeDispatch.C91>.Do(TypeDispatch.C91 value) => value.Value + 91;
        int IThing<TypeDispatch.C92>.Do(TypeDispatch.C92 value) => value.Value + 92;
        int IThing<TypeDispatch.C93>.Do(TypeDispatch.C93 value) => value.Value + 93;
        int IThing<TypeDispatch.C94>.Do(TypeDispatch.C94 value) => value.Value + 94;
        int IThing<TypeDispatch.C95>.Do(TypeDispatch.C95 value) => value.Value + 95;
        int IThing<TypeDispatch.C96>.Do(TypeDispatch.C96 value) => value.Value + 96;
        int IThing<TypeDispatch.C97>.Do(TypeDispatch.C97 value) => value.Value + 97;
        int IThing<TypeDispatch.C98>.Do(TypeDispatch.C98 value) => value.Value + 98;
        int IThing<TypeDispatch.C99>.Do(TypeDispatch.C99 value) => value.Value + 99;
        int IThing<TypeDispatch.C100>.Do(TypeDispatch.C100 value) => value.Value + 100;
        int IThing<TypeDispatch.C101>.Do(TypeDispatch.C101 value) => value.Value + 101;
        int IThing<TypeDispatch.C102>.Do(TypeDispatch.C102 value) => value.Value + 102;
        int IThing<TypeDispatch.C103>.Do(TypeDispatch.C103 value) => value.Value + 103;
        int IThing<TypeDispatch.C104>.Do(TypeDispatch.C104 value) => value.Value + 104;
        int IThing<TypeDispatch.C105>.Do(TypeDispatch.C105 value) => value.Value + 105;
        int IThing<TypeDispatch.C106>.Do(TypeDispatch.C106 value) => value.Value + 106;
        int IThing<TypeDispatch.C107>.Do(TypeDispatch.C107 value) => value.Value + 107;
        int IThing<TypeDispatch.C108>.Do(TypeDispatch.C108 value) => value.Value + 108;
        int IThing<TypeDispatch.C109>.Do(TypeDispatch.C109 value) => value.Value + 109;
        int IThing<TypeDispatch.C110>.Do(TypeDispatch.C110 value) => value.Value + 110;
        int IThing<TypeDispatch.C111>.Do(TypeDispatch.C111 value) => value.Value + 111;
        int IThing<TypeDispatch.C112>.Do(TypeDispatch.C112 value) => value.Value + 112;
        int IThing<TypeDispatch.C113>.Do(TypeDispatch.C113 value) => value.Value + 113;
        int IThing<TypeDispatch.C114>.Do(TypeDispatch.C114 value) => value.Value + 114;
        int IThing<TypeDispatch.C115>.Do(TypeDispatch.C115 value) => value.Value + 115;
        int IThing<TypeDispatch.C116>.Do(TypeDispatch.C116 value) => value.Value + 116;
        int IThing<TypeDispatch.C117>.Do(TypeDispatch.C117 value) => value.Value + 117;
        int IThing<TypeDispatch.C118>.Do(TypeDispatch.C118 value) => value.Value + 118;
        int IThing<TypeDispatch.C119>.Do(TypeDispatch.C119 value) => value.Value + 119;
        int IThing<TypeDispatch.C120>.Do(TypeDispatch.C120 value) => value.Value + 120;
        int IThing<TypeDispatch.C121>.Do(TypeDispatch.C121 value) => value.Value + 121;
        int IThing<TypeDispatch.C122>.Do(TypeDispatch.C122 value) => value.Value + 122;
        int IThing<TypeDispatch.C123>.Do(TypeDispatch.C123 value) => value.Value + 123;
        int IThing<TypeDispatch.C124>.Do(TypeDispatch.C124 value) => value.Value + 124;
        int IThing<TypeDispatch.C125>.Do(TypeDispatch.C125 value) => value.Value + 125;
        int IThing<TypeDispatch.C126>.Do(TypeDispatch.C126 value) => value.Value + 126;
        int IThing<TypeDispatch.C127>.Do(TypeDispatch.C127 value) => value.Value + 127;
        int IThing<TypeDispatch.C128>.Do(TypeDispatch.C128 value) => value.Value + 128;
        int IThing<TypeDispatch.C129>.Do(TypeDispatch.C129 value) => value.Value + 129;
        int IThing<TypeDispatch.C130>.Do(TypeDispatch.C130 value) => value.Value + 130;
        int IThing<TypeDispatch.C131>.Do(TypeDispatch.C131 value) => value.Value + 131;
        int IThing<TypeDispatch.C132>.Do(TypeDispatch.C132 value) => value.Value + 132;
        int IThing<TypeDispatch.C133>.Do(TypeDispatch.C133 value) => value.Value + 133;
        int IThing<TypeDispatch.C134>.Do(TypeDispatch.C134 value) => value.Value + 134;
        int IThing<TypeDispatch.C135>.Do(TypeDispatch.C135 value) => value.Value + 135;
        int IThing<TypeDispatch.C136>.Do(TypeDispatch.C136 value) => value.Value + 136;
        int IThing<TypeDispatch.C137>.Do(TypeDispatch.C137 value) => value.Value + 137;
        int IThing<TypeDispatch.C138>.Do(TypeDispatch.C138 value) => value.Value + 138;
        int IThing<TypeDispatch.C139>.Do(TypeDispatch.C139 value) => value.Value + 139;
        int IThing<TypeDispatch.C140>.Do(TypeDispatch.C140 value) => value.Value + 140;
        int IThing<TypeDispatch.C141>.Do(TypeDispatch.C141 value) => value.Value + 141;
        int IThing<TypeDispatch.C142>.Do(TypeDispatch.C142 value) => value.Value + 142;
        int IThing<TypeDispatch.C143>.Do(TypeDispatch.C143 value) => value.Value + 143;
        int IThing<TypeDispatch.C144>.Do(TypeDispatch.C144 value) => value.Value + 144;
        int IThing<TypeDispatch.C145>.Do(TypeDispatch.C145 value) => value.Value + 145;
        int IThing<TypeDispatch.C146>.Do(TypeDispatch.C146 value) => value.Value + 146;
        int IThing<TypeDispatch.C147>.Do(TypeDispatch.C147 value) => value.Value + 147;
        int IThing<TypeDispatch.C148>.Do(TypeDispatch.C148 value) => value.Value + 148;
        int IThing<TypeDispatch.C149>.Do(TypeDispatch.C149 value) => value.Value + 149;
        int IThing<TypeDispatch.C150>.Do(TypeDispatch.C150 value) => value.Value + 150;
        int IThing<TypeDispatch.C151>.Do(TypeDispatch.C151 value) => value.Value + 151;
        int IThing<TypeDispatch.C152>.Do(TypeDispatch.C152 value) => value.Value + 152;
        int IThing<TypeDispatch.C153>.Do(TypeDispatch.C153 value) => value.Value + 153;
        int IThing<TypeDispatch.C154>.Do(TypeDispatch.C154 value) => value.Value + 154;
        int IThing<TypeDispatch.C155>.Do(TypeDispatch.C155 value) => value.Value + 155;
        int IThing<TypeDispatch.C156>.Do(TypeDispatch.C156 value) => value.Value + 156;
        int IThing<TypeDispatch.C157>.Do(TypeDispatch.C157 value) => value.Value + 157;
        int IThing<TypeDispatch.C158>.Do(TypeDispatch.C158 value) => value.Value + 158;
        int IThing<TypeDispatch.C159>.Do(TypeDispatch.C159 value) => value.Value + 159;
        int IThing<TypeDispatch.C160>.Do(TypeDispatch.C160 value) => value.Value + 160;
        int IThing<TypeDispatch.C161>.Do(TypeDispatch.C161 value) => value.Value + 161;
        int IThing<TypeDispatch.C162>.Do(TypeDispatch.C162 value) => value.Value + 162;
        int IThing<TypeDispatch.C163>.Do(TypeDispatch.C163 value) => value.Value + 163;
        int IThing<TypeDispatch.C164>.Do(TypeDispatch.C164 value) => value.Value + 164;
        int IThing<TypeDispatch.C165>.Do(TypeDispatch.C165 value) => value.Value + 165;
        int IThing<TypeDispatch.C166>.Do(TypeDispatch.C166 value) => value.Value + 166;
        int IThing<TypeDispatch.C167>.Do(TypeDispatch.C167 value) => value.Value + 167;
        int IThing<TypeDispatch.C168>.Do(TypeDispatch.C168 value) => value.Value + 168;
        int IThing<TypeDispatch.C169>.Do(TypeDispatch.C169 value) => value.Value + 169;
        int IThing<TypeDispatch.C170>.Do(TypeDispatch.C170 value) => value.Value + 170;
        int IThing<TypeDispatch.C171>.Do(TypeDispatch.C171 value) => value.Value + 171;
        int IThing<TypeDispatch.C172>.Do(TypeDispatch.C172 value) => value.Value + 172;
        int IThing<TypeDispatch.C173>.Do(TypeDispatch.C173 value) => value.Value + 173;
        int IThing<TypeDispatch.C174>.Do(TypeDispatch.C174 value) => value.Value + 174;
        int IThing<TypeDispatch.C175>.Do(TypeDispatch.C175 value) => value.Value + 175;
        int IThing<TypeDispatch.C176>.Do(TypeDispatch.C176 value) => value.Value + 176;
        int IThing<TypeDispatch.C177>.Do(TypeDispatch.C177 value) => value.Value + 177;
        int IThing<TypeDispatch.C178>.Do(TypeDispatch.C178 value) => value.Value + 178;
        int IThing<TypeDispatch.C179>.Do(TypeDispatch.C179 value) => value.Value + 179;
        int IThing<TypeDispatch.C180>.Do(TypeDispatch.C180 value) => value.Value + 180;
        int IThing<TypeDispatch.C181>.Do(TypeDispatch.C181 value) => value.Value + 181;
        int IThing<TypeDispatch.C182>.Do(TypeDispatch.C182 value) => value.Value + 182;
        int IThing<TypeDispatch.C183>.Do(TypeDispatch.C183 value) => value.Value + 183;
        int IThing<TypeDispatch.C184>.Do(TypeDispatch.C184 value) => value.Value + 184;
        int IThing<TypeDispatch.C185>.Do(TypeDispatch.C185 value) => value.Value + 185;
        int IThing<TypeDispatch.C186>.Do(TypeDispatch.C186 value) => value.Value + 186;
        int IThing<TypeDispatch.C187>.Do(TypeDispatch.C187 value) => value.Value + 187;
        int IThing<TypeDispatch.C188>.Do(TypeDispatch.C188 value) => value.Value + 188;
        int IThing<TypeDispatch.C189>.Do(TypeDispatch.C189 value) => value.Value + 189;
        int IThing<TypeDispatch.C190>.Do(TypeDispatch.C190 value) => value.Value + 190;
        int IThing<TypeDispatch.C191>.Do(TypeDispatch.C191 value) => value.Value + 191;
        int IThing<TypeDispatch.C192>.Do(TypeDispatch.C192 value) => value.Value + 192;
        int IThing<TypeDispatch.C193>.Do(TypeDispatch.C193 value) => value.Value + 193;
        int IThing<TypeDispatch.C194>.Do(TypeDispatch.C194 value) => value.Value + 194;
        int IThing<TypeDispatch.C195>.Do(TypeDispatch.C195 value) => value.Value + 195;
        int IThing<TypeDispatch.C196>.Do(TypeDispatch.C196 value) => value.Value + 196;
        int IThing<TypeDispatch.C197>.Do(TypeDispatch.C197 value) => value.Value + 197;
        int IThing<TypeDispatch.C198>.Do(TypeDispatch.C198 value) => value.Value + 198;
        int IThing<TypeDispatch.C199>.Do(TypeDispatch.C199 value) => value.Value + 199;
        int IThing<TypeDispatch.C200>.Do(TypeDispatch.C200 value) => value.Value + 200;
        int IThing<TypeDispatch.C201>.Do(TypeDispatch.C201 value) => value.Value + 201;
        int IThing<TypeDispatch.C202>.Do(TypeDispatch.C202 value) => value.Value + 202;
        int IThing<TypeDispatch.C203>.Do(TypeDispatch.C203 value) => value.Value + 203;
        int IThing<TypeDispatch.C204>.Do(TypeDispatch.C204 value) => value.Value + 204;
        int IThing<TypeDispatch.C205>.Do(TypeDispatch.C205 value) => value.Value + 205;
        int IThing<TypeDispatch.C206>.Do(TypeDispatch.C206 value) => value.Value + 206;
        int IThing<TypeDispatch.C207>.Do(TypeDispatch.C207 value) => value.Value + 207;
        int IThing<TypeDispatch.C208>.Do(TypeDispatch.C208 value) => value.Value + 208;
        int IThing<TypeDispatch.C209>.Do(TypeDispatch.C209 value) => value.Value + 209;
        int IThing<TypeDispatch.C210>.Do(TypeDispatch.C210 value) => value.Value + 210;
        int IThing<TypeDispatch.C211>.Do(TypeDispatch.C211 value) => value.Value + 211;
        int IThing<TypeDispatch.C212>.Do(TypeDispatch.C212 value) => value.Value + 212;
        int IThing<TypeDispatch.C213>.Do(TypeDispatch.C213 value) => value.Value + 213;
        int IThing<TypeDispatch.C214>.Do(TypeDispatch.C214 value) => value.Value + 214;
        int IThing<TypeDispatch.C215>.Do(TypeDispatch.C215 value) => value.Value + 215;
        int IThing<TypeDispatch.C216>.Do(TypeDispatch.C216 value) => value.Value + 216;
        int IThing<TypeDispatch.C217>.Do(TypeDispatch.C217 value) => value.Value + 217;
        int IThing<TypeDispatch.C218>.Do(TypeDispatch.C218 value) => value.Value + 218;
        int IThing<TypeDispatch.C219>.Do(TypeDispatch.C219 value) => value.Value + 219;
        int IThing<TypeDispatch.C220>.Do(TypeDispatch.C220 value) => value.Value + 220;
        int IThing<TypeDispatch.C221>.Do(TypeDispatch.C221 value) => value.Value + 221;
        int IThing<TypeDispatch.C222>.Do(TypeDispatch.C222 value) => value.Value + 222;
        int IThing<TypeDispatch.C223>.Do(TypeDispatch.C223 value) => value.Value + 223;
        int IThing<TypeDispatch.C224>.Do(TypeDispatch.C224 value) => value.Value + 224;
        int IThing<TypeDispatch.C225>.Do(TypeDispatch.C225 value) => value.Value + 225;
        int IThing<TypeDispatch.C226>.Do(TypeDispatch.C226 value) => value.Value + 226;
        int IThing<TypeDispatch.C227>.Do(TypeDispatch.C227 value) => value.Value + 227;
        int IThing<TypeDispatch.C228>.Do(TypeDispatch.C228 value) => value.Value + 228;
        int IThing<TypeDispatch.C229>.Do(TypeDispatch.C229 value) => value.Value + 229;
        int IThing<TypeDispatch.C230>.Do(TypeDispatch.C230 value) => value.Value + 230;
        int IThing<TypeDispatch.C231>.Do(TypeDispatch.C231 value) => value.Value + 231;
        int IThing<TypeDispatch.C232>.Do(TypeDispatch.C232 value) => value.Value + 232;
        int IThing<TypeDispatch.C233>.Do(TypeDispatch.C233 value) => value.Value + 233;
        int IThing<TypeDispatch.C234>.Do(TypeDispatch.C234 value) => value.Value + 234;
        int IThing<TypeDispatch.C235>.Do(TypeDispatch.C235 value) => value.Value + 235;
        int IThing<TypeDispatch.C236>.Do(TypeDispatch.C236 value) => value.Value + 236;
        int IThing<TypeDispatch.C237>.Do(TypeDispatch.C237 value) => value.Value + 237;
        int IThing<TypeDispatch.C238>.Do(TypeDispatch.C238 value) => value.Value + 238;
        int IThing<TypeDispatch.C239>.Do(TypeDispatch.C239 value) => value.Value + 239;
        int IThing<TypeDispatch.C240>.Do(TypeDispatch.C240 value) => value.Value + 240;
        int IThing<TypeDispatch.C241>.Do(TypeDispatch.C241 value) => value.Value + 241;
        int IThing<TypeDispatch.C242>.Do(TypeDispatch.C242 value) => value.Value + 242;
        int IThing<TypeDispatch.C243>.Do(TypeDispatch.C243 value) => value.Value + 243;
        int IThing<TypeDispatch.C244>.Do(TypeDispatch.C244 value) => value.Value + 244;
        int IThing<TypeDispatch.C245>.Do(TypeDispatch.C245 value) => value.Value + 245;
        int IThing<TypeDispatch.C246>.Do(TypeDispatch.C246 value) => value.Value + 246;
        int IThing<TypeDispatch.C247>.Do(TypeDispatch.C247 value) => value.Value + 247;
        int IThing<TypeDispatch.C248>.Do(TypeDispatch.C248 value) => value.Value + 248;
        int IThing<TypeDispatch.C249>.Do(TypeDispatch.C249 value) => value.Value + 249;
        int IThing<TypeDispatch.C250>.Do(TypeDispatch.C250 value) => value.Value + 250;
        int IThing<TypeDispatch.C251>.Do(TypeDispatch.C251 value) => value.Value + 251;
        int IThing<TypeDispatch.C252>.Do(TypeDispatch.C252 value) => value.Value + 252;
        int IThing<TypeDispatch.C253>.Do(TypeDispatch.C253 value) => value.Value + 253;
        int IThing<TypeDispatch.C254>.Do(TypeDispatch.C254 value) => value.Value + 254;
        int IThing<TypeDispatch.C255>.Do(TypeDispatch.C255 value) => value.Value + 255;
        int IThing<TypeDispatch.C256>.Do(TypeDispatch.C256 value) => value.Value + 256;
        int IThing<TypeDispatch.C257>.Do(TypeDispatch.C257 value) => value.Value + 257;
        int IThing<TypeDispatch.C258>.Do(TypeDispatch.C258 value) => value.Value + 258;
        int IThing<TypeDispatch.C259>.Do(TypeDispatch.C259 value) => value.Value + 259;
        int IThing<TypeDispatch.C260>.Do(TypeDispatch.C260 value) => value.Value + 260;
        int IThing<TypeDispatch.C261>.Do(TypeDispatch.C261 value) => value.Value + 261;
        int IThing<TypeDispatch.C262>.Do(TypeDispatch.C262 value) => value.Value + 262;
        int IThing<TypeDispatch.C263>.Do(TypeDispatch.C263 value) => value.Value + 263;
        int IThing<TypeDispatch.C264>.Do(TypeDispatch.C264 value) => value.Value + 264;
        int IThing<TypeDispatch.C265>.Do(TypeDispatch.C265 value) => value.Value + 265;
        int IThing<TypeDispatch.C266>.Do(TypeDispatch.C266 value) => value.Value + 266;
        int IThing<TypeDispatch.C267>.Do(TypeDispatch.C267 value) => value.Value + 267;
        int IThing<TypeDispatch.C268>.Do(TypeDispatch.C268 value) => value.Value + 268;
        int IThing<TypeDispatch.C269>.Do(TypeDispatch.C269 value) => value.Value + 269;
        int IThing<TypeDispatch.C270>.Do(TypeDispatch.C270 value) => value.Value + 270;
        int IThing<TypeDispatch.C271>.Do(TypeDispatch.C271 value) => value.Value + 271;
        int IThing<TypeDispatch.C272>.Do(TypeDispatch.C272 value) => value.Value + 272;
        int IThing<TypeDispatch.C273>.Do(TypeDispatch.C273 value) => value.Value + 273;
        int IThing<TypeDispatch.C274>.Do(TypeDispatch.C274 value) => value.Value + 274;
        int IThing<TypeDispatch.C275>.Do(TypeDispatch.C275 value) => value.Value + 275;
        int IThing<TypeDispatch.C276>.Do(TypeDispatch.C276 value) => value.Value + 276;
        int IThing<TypeDispatch.C277>.Do(TypeDispatch.C277 value) => value.Value + 277;
        int IThing<TypeDispatch.C278>.Do(TypeDispatch.C278 value) => value.Value + 278;
        int IThing<TypeDispatch.C279>.Do(TypeDispatch.C279 value) => value.Value + 279;
        int IThing<TypeDispatch.C280>.Do(TypeDispatch.C280 value) => value.Value + 280;
        int IThing<TypeDispatch.C281>.Do(TypeDispatch.C281 value) => value.Value + 281;
        int IThing<TypeDispatch.C282>.Do(TypeDispatch.C282 value) => value.Value + 282;
        int IThing<TypeDispatch.C283>.Do(TypeDispatch.C283 value) => value.Value + 283;
        int IThing<TypeDispatch.C284>.Do(TypeDispatch.C284 value) => value.Value + 284;
        int IThing<TypeDispatch.C285>.Do(TypeDispatch.C285 value) => value.Value + 285;
        int IThing<TypeDispatch.C286>.Do(TypeDispatch.C286 value) => value.Value + 286;
        int IThing<TypeDispatch.C287>.Do(TypeDispatch.C287 value) => value.Value + 287;
        int IThing<TypeDispatch.C288>.Do(TypeDispatch.C288 value) => value.Value + 288;
        int IThing<TypeDispatch.C289>.Do(TypeDispatch.C289 value) => value.Value + 289;
        int IThing<TypeDispatch.C290>.Do(TypeDispatch.C290 value) => value.Value + 290;
        int IThing<TypeDispatch.C291>.Do(TypeDispatch.C291 value) => value.Value + 291;
        int IThing<TypeDispatch.C292>.Do(TypeDispatch.C292 value) => value.Value + 292;
        int IThing<TypeDispatch.C293>.Do(TypeDispatch.C293 value) => value.Value + 293;
        int IThing<TypeDispatch.C294>.Do(TypeDispatch.C294 value) => value.Value + 294;
        int IThing<TypeDispatch.C295>.Do(TypeDispatch.C295 value) => value.Value + 295;
        int IThing<TypeDispatch.C296>.Do(TypeDispatch.C296 value) => value.Value + 296;
        int IThing<TypeDispatch.C297>.Do(TypeDispatch.C297 value) => value.Value + 297;
        int IThing<TypeDispatch.C298>.Do(TypeDispatch.C298 value) => value.Value + 298;
        int IThing<TypeDispatch.C299>.Do(TypeDispatch.C299 value) => value.Value + 299;
        int IThing<TypeDispatch.C300>.Do(TypeDispatch.C300 value) => value.Value + 300;
        int IThing<TypeDispatch.C301>.Do(TypeDispatch.C301 value) => value.Value + 301;
        int IThing<TypeDispatch.C302>.Do(TypeDispatch.C302 value) => value.Value + 302;
        int IThing<TypeDispatch.C303>.Do(TypeDispatch.C303 value) => value.Value + 303;
        int IThing<TypeDispatch.C304>.Do(TypeDispatch.C304 value) => value.Value + 304;
        int IThing<TypeDispatch.C305>.Do(TypeDispatch.C305 value) => value.Value + 305;
        int IThing<TypeDispatch.C306>.Do(TypeDispatch.C306 value) => value.Value + 306;
        int IThing<TypeDispatch.C307>.Do(TypeDispatch.C307 value) => value.Value + 307;
        int IThing<TypeDispatch.C308>.Do(TypeDispatch.C308 value) => value.Value + 308;
        int IThing<TypeDispatch.C309>.Do(TypeDispatch.C309 value) => value.Value + 309;
        int IThing<TypeDispatch.C310>.Do(TypeDispatch.C310 value) => value.Value + 310;
        int IThing<TypeDispatch.C311>.Do(TypeDispatch.C311 value) => value.Value + 311;
        int IThing<TypeDispatch.C312>.Do(TypeDispatch.C312 value) => value.Value + 312;
        int IThing<TypeDispatch.C313>.Do(TypeDispatch.C313 value) => value.Value + 313;
        int IThing<TypeDispatch.C314>.Do(TypeDispatch.C314 value) => value.Value + 314;
        int IThing<TypeDispatch.C315>.Do(TypeDispatch.C315 value) => value.Value + 315;
        int IThing<TypeDispatch.C316>.Do(TypeDispatch.C316 value) => value.Value + 316;
        int IThing<TypeDispatch.C317>.Do(TypeDispatch.C317 value) => value.Value + 317;
        int IThing<TypeDispatch.C318>.Do(TypeDispatch.C318 value) => value.Value + 318;
        int IThing<TypeDispatch.C319>.Do(TypeDispatch.C319 value) => value.Value + 319;
        int IThing<TypeDispatch.C320>.Do(TypeDispatch.C320 value) => value.Value + 320;
        int IThing<TypeDispatch.C321>.Do(TypeDispatch.C321 value) => value.Value + 321;
        int IThing<TypeDispatch.C322>.Do(TypeDispatch.C322 value) => value.Value + 322;
        int IThing<TypeDispatch.C323>.Do(TypeDispatch.C323 value) => value.Value + 323;
        int IThing<TypeDispatch.C324>.Do(TypeDispatch.C324 value) => value.Value + 324;
        int IThing<TypeDispatch.C325>.Do(TypeDispatch.C325 value) => value.Value + 325;
        int IThing<TypeDispatch.C326>.Do(TypeDispatch.C326 value) => value.Value + 326;
        int IThing<TypeDispatch.C327>.Do(TypeDispatch.C327 value) => value.Value + 327;
        int IThing<TypeDispatch.C328>.Do(TypeDispatch.C328 value) => value.Value + 328;
        int IThing<TypeDispatch.C329>.Do(TypeDispatch.C329 value) => value.Value + 329;
        int IThing<TypeDispatch.C330>.Do(TypeDispatch.C330 value) => value.Value + 330;
        int IThing<TypeDispatch.C331>.Do(TypeDispatch.C331 value) => value.Value + 331;
        int IThing<TypeDispatch.C332>.Do(TypeDispatch.C332 value) => value.Value + 332;
        int IThing<TypeDispatch.C333>.Do(TypeDispatch.C333 value) => value.Value + 333;
        int IThing<TypeDispatch.C334>.Do(TypeDispatch.C334 value) => value.Value + 334;
        int IThing<TypeDispatch.C335>.Do(TypeDispatch.C335 value) => value.Value + 335;
        int IThing<TypeDispatch.C336>.Do(TypeDispatch.C336 value) => value.Value + 336;
        int IThing<TypeDispatch.C337>.Do(TypeDispatch.C337 value) => value.Value + 337;
        int IThing<TypeDispatch.C338>.Do(TypeDispatch.C338 value) => value.Value + 338;
        int IThing<TypeDispatch.C339>.Do(TypeDispatch.C339 value) => value.Value + 339;
        int IThing<TypeDispatch.C340>.Do(TypeDispatch.C340 value) => value.Value + 340;
        int IThing<TypeDispatch.C341>.Do(TypeDispatch.C341 value) => value.Value + 341;
        int IThing<TypeDispatch.C342>.Do(TypeDispatch.C342 value) => value.Value + 342;
        int IThing<TypeDispatch.C343>.Do(TypeDispatch.C343 value) => value.Value + 343;
        int IThing<TypeDispatch.C344>.Do(TypeDispatch.C344 value) => value.Value + 344;
        int IThing<TypeDispatch.C345>.Do(TypeDispatch.C345 value) => value.Value + 345;
        int IThing<TypeDispatch.C346>.Do(TypeDispatch.C346 value) => value.Value + 346;
        int IThing<TypeDispatch.C347>.Do(TypeDispatch.C347 value) => value.Value + 347;
        int IThing<TypeDispatch.C348>.Do(TypeDispatch.C348 value) => value.Value + 348;
        int IThing<TypeDispatch.C349>.Do(TypeDispatch.C349 value) => value.Value + 349;
        int IThing<TypeDispatch.C350>.Do(TypeDispatch.C350 value) => value.Value + 350;
        int IThing<TypeDispatch.C351>.Do(TypeDispatch.C351 value) => value.Value + 351;
        int IThing<TypeDispatch.C352>.Do(TypeDispatch.C352 value) => value.Value + 352;
        int IThing<TypeDispatch.C353>.Do(TypeDispatch.C353 value) => value.Value + 353;
        int IThing<TypeDispatch.C354>.Do(TypeDispatch.C354 value) => value.Value + 354;
        int IThing<TypeDispatch.C355>.Do(TypeDispatch.C355 value) => value.Value + 355;
        int IThing<TypeDispatch.C356>.Do(TypeDispatch.C356 value) => value.Value + 356;
        int IThing<TypeDispatch.C357>.Do(TypeDispatch.C357 value) => value.Value + 357;
        int IThing<TypeDispatch.C358>.Do(TypeDispatch.C358 value) => value.Value + 358;
        int IThing<TypeDispatch.C359>.Do(TypeDispatch.C359 value) => value.Value + 359;
        int IThing<TypeDispatch.C360>.Do(TypeDispatch.C360 value) => value.Value + 360;
        int IThing<TypeDispatch.C361>.Do(TypeDispatch.C361 value) => value.Value + 361;
        int IThing<TypeDispatch.C362>.Do(TypeDispatch.C362 value) => value.Value + 362;
        int IThing<TypeDispatch.C363>.Do(TypeDispatch.C363 value) => value.Value + 363;
        int IThing<TypeDispatch.C364>.Do(TypeDispatch.C364 value) => value.Value + 364;
        int IThing<TypeDispatch.C365>.Do(TypeDispatch.C365 value) => value.Value + 365;
        int IThing<TypeDispatch.C366>.Do(TypeDispatch.C366 value) => value.Value + 366;
        int IThing<TypeDispatch.C367>.Do(TypeDispatch.C367 value) => value.Value + 367;
        int IThing<TypeDispatch.C368>.Do(TypeDispatch.C368 value) => value.Value + 368;
        int IThing<TypeDispatch.C369>.Do(TypeDispatch.C369 value) => value.Value + 369;
        int IThing<TypeDispatch.C370>.Do(TypeDispatch.C370 value) => value.Value + 370;
        int IThing<TypeDispatch.C371>.Do(TypeDispatch.C371 value) => value.Value + 371;
        int IThing<TypeDispatch.C372>.Do(TypeDispatch.C372 value) => value.Value + 372;
        int IThing<TypeDispatch.C373>.Do(TypeDispatch.C373 value) => value.Value + 373;
        int IThing<TypeDispatch.C374>.Do(TypeDispatch.C374 value) => value.Value + 374;
        int IThing<TypeDispatch.C375>.Do(TypeDispatch.C375 value) => value.Value + 375;
        int IThing<TypeDispatch.C376>.Do(TypeDispatch.C376 value) => value.Value + 376;
        int IThing<TypeDispatch.C377>.Do(TypeDispatch.C377 value) => value.Value + 377;
        int IThing<TypeDispatch.C378>.Do(TypeDispatch.C378 value) => value.Value + 378;
        int IThing<TypeDispatch.C379>.Do(TypeDispatch.C379 value) => value.Value + 379;
        int IThing<TypeDispatch.C380>.Do(TypeDispatch.C380 value) => value.Value + 380;
        int IThing<TypeDispatch.C381>.Do(TypeDispatch.C381 value) => value.Value + 381;
        int IThing<TypeDispatch.C382>.Do(TypeDispatch.C382 value) => value.Value + 382;
        int IThing<TypeDispatch.C383>.Do(TypeDispatch.C383 value) => value.Value + 383;
        int IThing<TypeDispatch.C384>.Do(TypeDispatch.C384 value) => value.Value + 384;
        int IThing<TypeDispatch.C385>.Do(TypeDispatch.C385 value) => value.Value + 385;
        int IThing<TypeDispatch.C386>.Do(TypeDispatch.C386 value) => value.Value + 386;
        int IThing<TypeDispatch.C387>.Do(TypeDispatch.C387 value) => value.Value + 387;
        int IThing<TypeDispatch.C388>.Do(TypeDispatch.C388 value) => value.Value + 388;
        int IThing<TypeDispatch.C389>.Do(TypeDispatch.C389 value) => value.Value + 389;
        int IThing<TypeDispatch.C390>.Do(TypeDispatch.C390 value) => value.Value + 390;
        int IThing<TypeDispatch.C391>.Do(TypeDispatch.C391 value) => value.Value + 391;
        int IThing<TypeDispatch.C392>.Do(TypeDispatch.C392 value) => value.Value + 392;
        int IThing<TypeDispatch.C393>.Do(TypeDispatch.C393 value) => value.Value + 393;
        int IThing<TypeDispatch.C394>.Do(TypeDispatch.C394 value) => value.Value + 394;
        int IThing<TypeDispatch.C395>.Do(TypeDispatch.C395 value) => value.Value + 395;
        int IThing<TypeDispatch.C396>.Do(TypeDispatch.C396 value) => value.Value + 396;
        int IThing<TypeDispatch.C397>.Do(TypeDispatch.C397 value) => value.Value + 397;
        int IThing<TypeDispatch.C398>.Do(TypeDispatch.C398 value) => value.Value + 398;
        int IThing<TypeDispatch.C399>.Do(TypeDispatch.C399 value) => value.Value + 399;
        int IThing<TypeDispatch.C400>.Do(TypeDispatch.C400 value) => value.Value + 400;
        int IThing<TypeDispatch.C401>.Do(TypeDispatch.C401 value) => value.Value + 401;
        int IThing<TypeDispatch.C402>.Do(TypeDispatch.C402 value) => value.Value + 402;
        int IThing<TypeDispatch.C403>.Do(TypeDispatch.C403 value) => value.Value + 403;
        int IThing<TypeDispatch.C404>.Do(TypeDispatch.C404 value) => value.Value + 404;
        int IThing<TypeDispatch.C405>.Do(TypeDispatch.C405 value) => value.Value + 405;
        int IThing<TypeDispatch.C406>.Do(TypeDispatch.C406 value) => value.Value + 406;
        int IThing<TypeDispatch.C407>.Do(TypeDispatch.C407 value) => value.Value + 407;
        int IThing<TypeDispatch.C408>.Do(TypeDispatch.C408 value) => value.Value + 408;
        int IThing<TypeDispatch.C409>.Do(TypeDispatch.C409 value) => value.Value + 409;
        int IThing<TypeDispatch.C410>.Do(TypeDispatch.C410 value) => value.Value + 410;
        int IThing<TypeDispatch.C411>.Do(TypeDispatch.C411 value) => value.Value + 411;
        int IThing<TypeDispatch.C412>.Do(TypeDispatch.C412 value) => value.Value + 412;
        int IThing<TypeDispatch.C413>.Do(TypeDispatch.C413 value) => value.Value + 413;
        int IThing<TypeDispatch.C414>.Do(TypeDispatch.C414 value) => value.Value + 414;
        int IThing<TypeDispatch.C415>.Do(TypeDispatch.C415 value) => value.Value + 415;
        int IThing<TypeDispatch.C416>.Do(TypeDispatch.C416 value) => value.Value + 416;
        int IThing<TypeDispatch.C417>.Do(TypeDispatch.C417 value) => value.Value + 417;
        int IThing<TypeDispatch.C418>.Do(TypeDispatch.C418 value) => value.Value + 418;
        int IThing<TypeDispatch.C419>.Do(TypeDispatch.C419 value) => value.Value + 419;
        int IThing<TypeDispatch.C420>.Do(TypeDispatch.C420 value) => value.Value + 420;
        int IThing<TypeDispatch.C421>.Do(TypeDispatch.C421 value) => value.Value + 421;
        int IThing<TypeDispatch.C422>.Do(TypeDispatch.C422 value) => value.Value + 422;
        int IThing<TypeDispatch.C423>.Do(TypeDispatch.C423 value) => value.Value + 423;
        int IThing<TypeDispatch.C424>.Do(TypeDispatch.C424 value) => value.Value + 424;
        int IThing<TypeDispatch.C425>.Do(TypeDispatch.C425 value) => value.Value + 425;
        int IThing<TypeDispatch.C426>.Do(TypeDispatch.C426 value) => value.Value + 426;
        int IThing<TypeDispatch.C427>.Do(TypeDispatch.C427 value) => value.Value + 427;
        int IThing<TypeDispatch.C428>.Do(TypeDispatch.C428 value) => value.Value + 428;
        int IThing<TypeDispatch.C429>.Do(TypeDispatch.C429 value) => value.Value + 429;
        int IThing<TypeDispatch.C430>.Do(TypeDispatch.C430 value) => value.Value + 430;
        int IThing<TypeDispatch.C431>.Do(TypeDispatch.C431 value) => value.Value + 431;
        int IThing<TypeDispatch.C432>.Do(TypeDispatch.C432 value) => value.Value + 432;
        int IThing<TypeDispatch.C433>.Do(TypeDispatch.C433 value) => value.Value + 433;
        int IThing<TypeDispatch.C434>.Do(TypeDispatch.C434 value) => value.Value + 434;
        int IThing<TypeDispatch.C435>.Do(TypeDispatch.C435 value) => value.Value + 435;
        int IThing<TypeDispatch.C436>.Do(TypeDispatch.C436 value) => value.Value + 436;
        int IThing<TypeDispatch.C437>.Do(TypeDispatch.C437 value) => value.Value + 437;
        int IThing<TypeDispatch.C438>.Do(TypeDispatch.C438 value) => value.Value + 438;
        int IThing<TypeDispatch.C439>.Do(TypeDispatch.C439 value) => value.Value + 439;
        int IThing<TypeDispatch.C440>.Do(TypeDispatch.C440 value) => value.Value + 440;
        int IThing<TypeDispatch.C441>.Do(TypeDispatch.C441 value) => value.Value + 441;
        int IThing<TypeDispatch.C442>.Do(TypeDispatch.C442 value) => value.Value + 442;
        int IThing<TypeDispatch.C443>.Do(TypeDispatch.C443 value) => value.Value + 443;
        int IThing<TypeDispatch.C444>.Do(TypeDispatch.C444 value) => value.Value + 444;
        int IThing<TypeDispatch.C445>.Do(TypeDispatch.C445 value) => value.Value + 445;
        int IThing<TypeDispatch.C446>.Do(TypeDispatch.C446 value) => value.Value + 446;
        int IThing<TypeDispatch.C447>.Do(TypeDispatch.C447 value) => value.Value + 447;
        int IThing<TypeDispatch.C448>.Do(TypeDispatch.C448 value) => value.Value + 448;
        int IThing<TypeDispatch.C449>.Do(TypeDispatch.C449 value) => value.Value + 449;
        int IThing<TypeDispatch.C450>.Do(TypeDispatch.C450 value) => value.Value + 450;
        int IThing<TypeDispatch.C451>.Do(TypeDispatch.C451 value) => value.Value + 451;
        int IThing<TypeDispatch.C452>.Do(TypeDispatch.C452 value) => value.Value + 452;
        int IThing<TypeDispatch.C453>.Do(TypeDispatch.C453 value) => value.Value + 453;
        int IThing<TypeDispatch.C454>.Do(TypeDispatch.C454 value) => value.Value + 454;
        int IThing<TypeDispatch.C455>.Do(TypeDispatch.C455 value) => value.Value + 455;
        int IThing<TypeDispatch.C456>.Do(TypeDispatch.C456 value) => value.Value + 456;
        int IThing<TypeDispatch.C457>.Do(TypeDispatch.C457 value) => value.Value + 457;
        int IThing<TypeDispatch.C458>.Do(TypeDispatch.C458 value) => value.Value + 458;
        int IThing<TypeDispatch.C459>.Do(TypeDispatch.C459 value) => value.Value + 459;
        int IThing<TypeDispatch.C460>.Do(TypeDispatch.C460 value) => value.Value + 460;
        int IThing<TypeDispatch.C461>.Do(TypeDispatch.C461 value) => value.Value + 461;
        int IThing<TypeDispatch.C462>.Do(TypeDispatch.C462 value) => value.Value + 462;
        int IThing<TypeDispatch.C463>.Do(TypeDispatch.C463 value) => value.Value + 463;
        int IThing<TypeDispatch.C464>.Do(TypeDispatch.C464 value) => value.Value + 464;
        int IThing<TypeDispatch.C465>.Do(TypeDispatch.C465 value) => value.Value + 465;
        int IThing<TypeDispatch.C466>.Do(TypeDispatch.C466 value) => value.Value + 466;
        int IThing<TypeDispatch.C467>.Do(TypeDispatch.C467 value) => value.Value + 467;
        int IThing<TypeDispatch.C468>.Do(TypeDispatch.C468 value) => value.Value + 468;
        int IThing<TypeDispatch.C469>.Do(TypeDispatch.C469 value) => value.Value + 469;
        int IThing<TypeDispatch.C470>.Do(TypeDispatch.C470 value) => value.Value + 470;
        int IThing<TypeDispatch.C471>.Do(TypeDispatch.C471 value) => value.Value + 471;
        int IThing<TypeDispatch.C472>.Do(TypeDispatch.C472 value) => value.Value + 472;
        int IThing<TypeDispatch.C473>.Do(TypeDispatch.C473 value) => value.Value + 473;
        int IThing<TypeDispatch.C474>.Do(TypeDispatch.C474 value) => value.Value + 474;
        int IThing<TypeDispatch.C475>.Do(TypeDispatch.C475 value) => value.Value + 475;
        int IThing<TypeDispatch.C476>.Do(TypeDispatch.C476 value) => value.Value + 476;
        int IThing<TypeDispatch.C477>.Do(TypeDispatch.C477 value) => value.Value + 477;
        int IThing<TypeDispatch.C478>.Do(TypeDispatch.C478 value) => value.Value + 478;
        int IThing<TypeDispatch.C479>.Do(TypeDispatch.C479 value) => value.Value + 479;
        int IThing<TypeDispatch.C480>.Do(TypeDispatch.C480 value) => value.Value + 480;
        int IThing<TypeDispatch.C481>.Do(TypeDispatch.C481 value) => value.Value + 481;
        int IThing<TypeDispatch.C482>.Do(TypeDispatch.C482 value) => value.Value + 482;
        int IThing<TypeDispatch.C483>.Do(TypeDispatch.C483 value) => value.Value + 483;
        int IThing<TypeDispatch.C484>.Do(TypeDispatch.C484 value) => value.Value + 484;
        int IThing<TypeDispatch.C485>.Do(TypeDispatch.C485 value) => value.Value + 485;
        int IThing<TypeDispatch.C486>.Do(TypeDispatch.C486 value) => value.Value + 486;
        int IThing<TypeDispatch.C487>.Do(TypeDispatch.C487 value) => value.Value + 487;
        int IThing<TypeDispatch.C488>.Do(TypeDispatch.C488 value) => value.Value + 488;
        int IThing<TypeDispatch.C489>.Do(TypeDispatch.C489 value) => value.Value + 489;
        int IThing<TypeDispatch.C490>.Do(TypeDispatch.C490 value) => value.Value + 490;
        int IThing<TypeDispatch.C491>.Do(TypeDispatch.C491 value) => value.Value + 491;
        int IThing<TypeDispatch.C492>.Do(TypeDispatch.C492 value) => value.Value + 492;
        int IThing<TypeDispatch.C493>.Do(TypeDispatch.C493 value) => value.Value + 493;
        int IThing<TypeDispatch.C494>.Do(TypeDispatch.C494 value) => value.Value + 494;
        int IThing<TypeDispatch.C495>.Do(TypeDispatch.C495 value) => value.Value + 495;
        int IThing<TypeDispatch.C496>.Do(TypeDispatch.C496 value) => value.Value + 496;
        int IThing<TypeDispatch.C497>.Do(TypeDispatch.C497 value) => value.Value + 497;
        int IThing<TypeDispatch.C498>.Do(TypeDispatch.C498 value) => value.Value + 498;
        int IThing<TypeDispatch.C499>.Do(TypeDispatch.C499 value) => value.Value + 499;
        int IThing<TypeDispatch.C500>.Do(TypeDispatch.C500 value) => value.Value + 500;
        int IThing<TypeDispatch.C501>.Do(TypeDispatch.C501 value) => value.Value + 501;
        int IThing<TypeDispatch.C502>.Do(TypeDispatch.C502 value) => value.Value + 502;
        int IThing<TypeDispatch.C503>.Do(TypeDispatch.C503 value) => value.Value + 503;
        int IThing<TypeDispatch.C504>.Do(TypeDispatch.C504 value) => value.Value + 504;
        int IThing<TypeDispatch.C505>.Do(TypeDispatch.C505 value) => value.Value + 505;
        int IThing<TypeDispatch.C506>.Do(TypeDispatch.C506 value) => value.Value + 506;
        int IThing<TypeDispatch.C507>.Do(TypeDispatch.C507 value) => value.Value + 507;
        int IThing<TypeDispatch.C508>.Do(TypeDispatch.C508 value) => value.Value + 508;
        int IThing<TypeDispatch.C509>.Do(TypeDispatch.C509 value) => value.Value + 509;
        int IThing<TypeDispatch.C510>.Do(TypeDispatch.C510 value) => value.Value + 510;
        int IThing<TypeDispatch.C511>.Do(TypeDispatch.C511 value) => value.Value + 511;

        /// <summary>Per-model registration: one assignment per contract, at construction.</summary>
        public void Register()
        {
            ModelOf512.Helper<TypeDispatch.C0>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C1>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C2>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C3>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C4>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C5>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C6>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C7>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C8>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C9>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C10>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C11>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C12>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C13>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C14>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C15>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C16>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C17>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C18>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C19>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C20>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C21>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C22>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C23>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C24>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C25>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C26>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C27>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C28>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C29>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C30>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C31>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C32>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C33>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C34>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C35>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C36>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C37>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C38>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C39>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C40>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C41>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C42>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C43>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C44>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C45>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C46>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C47>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C48>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C49>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C50>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C51>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C52>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C53>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C54>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C55>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C56>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C57>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C58>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C59>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C60>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C61>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C62>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C63>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C64>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C65>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C66>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C67>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C68>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C69>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C70>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C71>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C72>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C73>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C74>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C75>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C76>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C77>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C78>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C79>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C80>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C81>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C82>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C83>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C84>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C85>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C86>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C87>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C88>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C89>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C90>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C91>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C92>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C93>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C94>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C95>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C96>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C97>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C98>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C99>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C100>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C101>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C102>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C103>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C104>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C105>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C106>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C107>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C108>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C109>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C110>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C111>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C112>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C113>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C114>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C115>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C116>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C117>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C118>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C119>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C120>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C121>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C122>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C123>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C124>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C125>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C126>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C127>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C128>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C129>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C130>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C131>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C132>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C133>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C134>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C135>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C136>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C137>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C138>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C139>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C140>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C141>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C142>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C143>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C144>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C145>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C146>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C147>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C148>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C149>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C150>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C151>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C152>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C153>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C154>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C155>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C156>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C157>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C158>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C159>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C160>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C161>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C162>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C163>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C164>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C165>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C166>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C167>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C168>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C169>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C170>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C171>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C172>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C173>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C174>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C175>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C176>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C177>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C178>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C179>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C180>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C181>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C182>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C183>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C184>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C185>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C186>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C187>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C188>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C189>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C190>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C191>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C192>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C193>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C194>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C195>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C196>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C197>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C198>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C199>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C200>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C201>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C202>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C203>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C204>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C205>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C206>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C207>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C208>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C209>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C210>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C211>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C212>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C213>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C214>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C215>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C216>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C217>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C218>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C219>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C220>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C221>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C222>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C223>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C224>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C225>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C226>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C227>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C228>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C229>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C230>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C231>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C232>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C233>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C234>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C235>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C236>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C237>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C238>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C239>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C240>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C241>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C242>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C243>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C244>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C245>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C246>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C247>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C248>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C249>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C250>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C251>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C252>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C253>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C254>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C255>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C256>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C257>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C258>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C259>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C260>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C261>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C262>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C263>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C264>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C265>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C266>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C267>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C268>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C269>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C270>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C271>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C272>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C273>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C274>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C275>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C276>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C277>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C278>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C279>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C280>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C281>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C282>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C283>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C284>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C285>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C286>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C287>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C288>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C289>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C290>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C291>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C292>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C293>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C294>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C295>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C296>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C297>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C298>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C299>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C300>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C301>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C302>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C303>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C304>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C305>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C306>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C307>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C308>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C309>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C310>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C311>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C312>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C313>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C314>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C315>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C316>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C317>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C318>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C319>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C320>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C321>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C322>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C323>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C324>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C325>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C326>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C327>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C328>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C329>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C330>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C331>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C332>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C333>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C334>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C335>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C336>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C337>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C338>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C339>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C340>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C341>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C342>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C343>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C344>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C345>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C346>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C347>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C348>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C349>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C350>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C351>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C352>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C353>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C354>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C355>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C356>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C357>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C358>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C359>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C360>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C361>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C362>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C363>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C364>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C365>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C366>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C367>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C368>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C369>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C370>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C371>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C372>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C373>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C374>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C375>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C376>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C377>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C378>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C379>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C380>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C381>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C382>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C383>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C384>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C385>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C386>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C387>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C388>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C389>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C390>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C391>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C392>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C393>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C394>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C395>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C396>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C397>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C398>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C399>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C400>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C401>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C402>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C403>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C404>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C405>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C406>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C407>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C408>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C409>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C410>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C411>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C412>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C413>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C414>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C415>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C416>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C417>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C418>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C419>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C420>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C421>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C422>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C423>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C424>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C425>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C426>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C427>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C428>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C429>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C430>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C431>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C432>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C433>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C434>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C435>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C436>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C437>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C438>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C439>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C440>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C441>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C442>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C443>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C444>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C445>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C446>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C447>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C448>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C449>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C450>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C451>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C452>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C453>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C454>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C455>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C456>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C457>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C458>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C459>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C460>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C461>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C462>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C463>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C464>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C465>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C466>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C467>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C468>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C469>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C470>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C471>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C472>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C473>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C474>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C475>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C476>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C477>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C478>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C479>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C480>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C481>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C482>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C483>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C484>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C485>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C486>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C487>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C488>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C489>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C490>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C491>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C492>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C493>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C494>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C495>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C496>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C497>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C498>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C499>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C500>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C501>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C502>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C503>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C504>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C505>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C506>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C507>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C508>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C509>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C510>.Instance = this;
            ModelOf512.Helper<TypeDispatch.C511>.Instance = this;
        }
    }

    /// <summary>Stands in for a generated model class; Helper is NESTED, so the slot is per-model.</summary>
    public sealed class ModelOf512
    {
        public static class Helper<T>
        {
            public static IThing<T> Instance;
        }
    }

}
#endif
