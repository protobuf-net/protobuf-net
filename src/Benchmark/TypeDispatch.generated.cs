// GENERATED - see TypeDispatchBenchmarks.cs for what this is for and how to regenerate. It is
// five hundred and twelve empty classes plus three sizes of dispatch chain; the shapes are the
// point rather than the contents, which is exactly why it is generated rather than hand-written.
//
// The size ladder is SMALL / MEDIUM / HUGE (8 / 64 / 512) rather than a narrow band: the strategies
// differ in how they SCALE, so a ladder that stops at 64 cannot distinguish an O(1) lookup from a
// short linear scan - which is the whole question.
#if NET8_0_OR_GREATER
using System;

namespace Benchmark
{
    public static partial class TypeDispatch
    {
        public sealed class C0 { public int Value; }
        public sealed class C1 { public int Value; }
        public sealed class C2 { public int Value; }
        public sealed class C3 { public int Value; }
        public sealed class C4 { public int Value; }
        public sealed class C5 { public int Value; }
        public sealed class C6 { public int Value; }
        public sealed class C7 { public int Value; }
        public sealed class C8 { public int Value; }
        public sealed class C9 { public int Value; }
        public sealed class C10 { public int Value; }
        public sealed class C11 { public int Value; }
        public sealed class C12 { public int Value; }
        public sealed class C13 { public int Value; }
        public sealed class C14 { public int Value; }
        public sealed class C15 { public int Value; }
        public sealed class C16 { public int Value; }
        public sealed class C17 { public int Value; }
        public sealed class C18 { public int Value; }
        public sealed class C19 { public int Value; }
        public sealed class C20 { public int Value; }
        public sealed class C21 { public int Value; }
        public sealed class C22 { public int Value; }
        public sealed class C23 { public int Value; }
        public sealed class C24 { public int Value; }
        public sealed class C25 { public int Value; }
        public sealed class C26 { public int Value; }
        public sealed class C27 { public int Value; }
        public sealed class C28 { public int Value; }
        public sealed class C29 { public int Value; }
        public sealed class C30 { public int Value; }
        public sealed class C31 { public int Value; }
        public sealed class C32 { public int Value; }
        public sealed class C33 { public int Value; }
        public sealed class C34 { public int Value; }
        public sealed class C35 { public int Value; }
        public sealed class C36 { public int Value; }
        public sealed class C37 { public int Value; }
        public sealed class C38 { public int Value; }
        public sealed class C39 { public int Value; }
        public sealed class C40 { public int Value; }
        public sealed class C41 { public int Value; }
        public sealed class C42 { public int Value; }
        public sealed class C43 { public int Value; }
        public sealed class C44 { public int Value; }
        public sealed class C45 { public int Value; }
        public sealed class C46 { public int Value; }
        public sealed class C47 { public int Value; }
        public sealed class C48 { public int Value; }
        public sealed class C49 { public int Value; }
        public sealed class C50 { public int Value; }
        public sealed class C51 { public int Value; }
        public sealed class C52 { public int Value; }
        public sealed class C53 { public int Value; }
        public sealed class C54 { public int Value; }
        public sealed class C55 { public int Value; }
        public sealed class C56 { public int Value; }
        public sealed class C57 { public int Value; }
        public sealed class C58 { public int Value; }
        public sealed class C59 { public int Value; }
        public sealed class C60 { public int Value; }
        public sealed class C61 { public int Value; }
        public sealed class C62 { public int Value; }
        public sealed class C63 { public int Value; }
        public sealed class C64 { public int Value; }
        public sealed class C65 { public int Value; }
        public sealed class C66 { public int Value; }
        public sealed class C67 { public int Value; }
        public sealed class C68 { public int Value; }
        public sealed class C69 { public int Value; }
        public sealed class C70 { public int Value; }
        public sealed class C71 { public int Value; }
        public sealed class C72 { public int Value; }
        public sealed class C73 { public int Value; }
        public sealed class C74 { public int Value; }
        public sealed class C75 { public int Value; }
        public sealed class C76 { public int Value; }
        public sealed class C77 { public int Value; }
        public sealed class C78 { public int Value; }
        public sealed class C79 { public int Value; }
        public sealed class C80 { public int Value; }
        public sealed class C81 { public int Value; }
        public sealed class C82 { public int Value; }
        public sealed class C83 { public int Value; }
        public sealed class C84 { public int Value; }
        public sealed class C85 { public int Value; }
        public sealed class C86 { public int Value; }
        public sealed class C87 { public int Value; }
        public sealed class C88 { public int Value; }
        public sealed class C89 { public int Value; }
        public sealed class C90 { public int Value; }
        public sealed class C91 { public int Value; }
        public sealed class C92 { public int Value; }
        public sealed class C93 { public int Value; }
        public sealed class C94 { public int Value; }
        public sealed class C95 { public int Value; }
        public sealed class C96 { public int Value; }
        public sealed class C97 { public int Value; }
        public sealed class C98 { public int Value; }
        public sealed class C99 { public int Value; }
        public sealed class C100 { public int Value; }
        public sealed class C101 { public int Value; }
        public sealed class C102 { public int Value; }
        public sealed class C103 { public int Value; }
        public sealed class C104 { public int Value; }
        public sealed class C105 { public int Value; }
        public sealed class C106 { public int Value; }
        public sealed class C107 { public int Value; }
        public sealed class C108 { public int Value; }
        public sealed class C109 { public int Value; }
        public sealed class C110 { public int Value; }
        public sealed class C111 { public int Value; }
        public sealed class C112 { public int Value; }
        public sealed class C113 { public int Value; }
        public sealed class C114 { public int Value; }
        public sealed class C115 { public int Value; }
        public sealed class C116 { public int Value; }
        public sealed class C117 { public int Value; }
        public sealed class C118 { public int Value; }
        public sealed class C119 { public int Value; }
        public sealed class C120 { public int Value; }
        public sealed class C121 { public int Value; }
        public sealed class C122 { public int Value; }
        public sealed class C123 { public int Value; }
        public sealed class C124 { public int Value; }
        public sealed class C125 { public int Value; }
        public sealed class C126 { public int Value; }
        public sealed class C127 { public int Value; }
        public sealed class C128 { public int Value; }
        public sealed class C129 { public int Value; }
        public sealed class C130 { public int Value; }
        public sealed class C131 { public int Value; }
        public sealed class C132 { public int Value; }
        public sealed class C133 { public int Value; }
        public sealed class C134 { public int Value; }
        public sealed class C135 { public int Value; }
        public sealed class C136 { public int Value; }
        public sealed class C137 { public int Value; }
        public sealed class C138 { public int Value; }
        public sealed class C139 { public int Value; }
        public sealed class C140 { public int Value; }
        public sealed class C141 { public int Value; }
        public sealed class C142 { public int Value; }
        public sealed class C143 { public int Value; }
        public sealed class C144 { public int Value; }
        public sealed class C145 { public int Value; }
        public sealed class C146 { public int Value; }
        public sealed class C147 { public int Value; }
        public sealed class C148 { public int Value; }
        public sealed class C149 { public int Value; }
        public sealed class C150 { public int Value; }
        public sealed class C151 { public int Value; }
        public sealed class C152 { public int Value; }
        public sealed class C153 { public int Value; }
        public sealed class C154 { public int Value; }
        public sealed class C155 { public int Value; }
        public sealed class C156 { public int Value; }
        public sealed class C157 { public int Value; }
        public sealed class C158 { public int Value; }
        public sealed class C159 { public int Value; }
        public sealed class C160 { public int Value; }
        public sealed class C161 { public int Value; }
        public sealed class C162 { public int Value; }
        public sealed class C163 { public int Value; }
        public sealed class C164 { public int Value; }
        public sealed class C165 { public int Value; }
        public sealed class C166 { public int Value; }
        public sealed class C167 { public int Value; }
        public sealed class C168 { public int Value; }
        public sealed class C169 { public int Value; }
        public sealed class C170 { public int Value; }
        public sealed class C171 { public int Value; }
        public sealed class C172 { public int Value; }
        public sealed class C173 { public int Value; }
        public sealed class C174 { public int Value; }
        public sealed class C175 { public int Value; }
        public sealed class C176 { public int Value; }
        public sealed class C177 { public int Value; }
        public sealed class C178 { public int Value; }
        public sealed class C179 { public int Value; }
        public sealed class C180 { public int Value; }
        public sealed class C181 { public int Value; }
        public sealed class C182 { public int Value; }
        public sealed class C183 { public int Value; }
        public sealed class C184 { public int Value; }
        public sealed class C185 { public int Value; }
        public sealed class C186 { public int Value; }
        public sealed class C187 { public int Value; }
        public sealed class C188 { public int Value; }
        public sealed class C189 { public int Value; }
        public sealed class C190 { public int Value; }
        public sealed class C191 { public int Value; }
        public sealed class C192 { public int Value; }
        public sealed class C193 { public int Value; }
        public sealed class C194 { public int Value; }
        public sealed class C195 { public int Value; }
        public sealed class C196 { public int Value; }
        public sealed class C197 { public int Value; }
        public sealed class C198 { public int Value; }
        public sealed class C199 { public int Value; }
        public sealed class C200 { public int Value; }
        public sealed class C201 { public int Value; }
        public sealed class C202 { public int Value; }
        public sealed class C203 { public int Value; }
        public sealed class C204 { public int Value; }
        public sealed class C205 { public int Value; }
        public sealed class C206 { public int Value; }
        public sealed class C207 { public int Value; }
        public sealed class C208 { public int Value; }
        public sealed class C209 { public int Value; }
        public sealed class C210 { public int Value; }
        public sealed class C211 { public int Value; }
        public sealed class C212 { public int Value; }
        public sealed class C213 { public int Value; }
        public sealed class C214 { public int Value; }
        public sealed class C215 { public int Value; }
        public sealed class C216 { public int Value; }
        public sealed class C217 { public int Value; }
        public sealed class C218 { public int Value; }
        public sealed class C219 { public int Value; }
        public sealed class C220 { public int Value; }
        public sealed class C221 { public int Value; }
        public sealed class C222 { public int Value; }
        public sealed class C223 { public int Value; }
        public sealed class C224 { public int Value; }
        public sealed class C225 { public int Value; }
        public sealed class C226 { public int Value; }
        public sealed class C227 { public int Value; }
        public sealed class C228 { public int Value; }
        public sealed class C229 { public int Value; }
        public sealed class C230 { public int Value; }
        public sealed class C231 { public int Value; }
        public sealed class C232 { public int Value; }
        public sealed class C233 { public int Value; }
        public sealed class C234 { public int Value; }
        public sealed class C235 { public int Value; }
        public sealed class C236 { public int Value; }
        public sealed class C237 { public int Value; }
        public sealed class C238 { public int Value; }
        public sealed class C239 { public int Value; }
        public sealed class C240 { public int Value; }
        public sealed class C241 { public int Value; }
        public sealed class C242 { public int Value; }
        public sealed class C243 { public int Value; }
        public sealed class C244 { public int Value; }
        public sealed class C245 { public int Value; }
        public sealed class C246 { public int Value; }
        public sealed class C247 { public int Value; }
        public sealed class C248 { public int Value; }
        public sealed class C249 { public int Value; }
        public sealed class C250 { public int Value; }
        public sealed class C251 { public int Value; }
        public sealed class C252 { public int Value; }
        public sealed class C253 { public int Value; }
        public sealed class C254 { public int Value; }
        public sealed class C255 { public int Value; }
        public sealed class C256 { public int Value; }
        public sealed class C257 { public int Value; }
        public sealed class C258 { public int Value; }
        public sealed class C259 { public int Value; }
        public sealed class C260 { public int Value; }
        public sealed class C261 { public int Value; }
        public sealed class C262 { public int Value; }
        public sealed class C263 { public int Value; }
        public sealed class C264 { public int Value; }
        public sealed class C265 { public int Value; }
        public sealed class C266 { public int Value; }
        public sealed class C267 { public int Value; }
        public sealed class C268 { public int Value; }
        public sealed class C269 { public int Value; }
        public sealed class C270 { public int Value; }
        public sealed class C271 { public int Value; }
        public sealed class C272 { public int Value; }
        public sealed class C273 { public int Value; }
        public sealed class C274 { public int Value; }
        public sealed class C275 { public int Value; }
        public sealed class C276 { public int Value; }
        public sealed class C277 { public int Value; }
        public sealed class C278 { public int Value; }
        public sealed class C279 { public int Value; }
        public sealed class C280 { public int Value; }
        public sealed class C281 { public int Value; }
        public sealed class C282 { public int Value; }
        public sealed class C283 { public int Value; }
        public sealed class C284 { public int Value; }
        public sealed class C285 { public int Value; }
        public sealed class C286 { public int Value; }
        public sealed class C287 { public int Value; }
        public sealed class C288 { public int Value; }
        public sealed class C289 { public int Value; }
        public sealed class C290 { public int Value; }
        public sealed class C291 { public int Value; }
        public sealed class C292 { public int Value; }
        public sealed class C293 { public int Value; }
        public sealed class C294 { public int Value; }
        public sealed class C295 { public int Value; }
        public sealed class C296 { public int Value; }
        public sealed class C297 { public int Value; }
        public sealed class C298 { public int Value; }
        public sealed class C299 { public int Value; }
        public sealed class C300 { public int Value; }
        public sealed class C301 { public int Value; }
        public sealed class C302 { public int Value; }
        public sealed class C303 { public int Value; }
        public sealed class C304 { public int Value; }
        public sealed class C305 { public int Value; }
        public sealed class C306 { public int Value; }
        public sealed class C307 { public int Value; }
        public sealed class C308 { public int Value; }
        public sealed class C309 { public int Value; }
        public sealed class C310 { public int Value; }
        public sealed class C311 { public int Value; }
        public sealed class C312 { public int Value; }
        public sealed class C313 { public int Value; }
        public sealed class C314 { public int Value; }
        public sealed class C315 { public int Value; }
        public sealed class C316 { public int Value; }
        public sealed class C317 { public int Value; }
        public sealed class C318 { public int Value; }
        public sealed class C319 { public int Value; }
        public sealed class C320 { public int Value; }
        public sealed class C321 { public int Value; }
        public sealed class C322 { public int Value; }
        public sealed class C323 { public int Value; }
        public sealed class C324 { public int Value; }
        public sealed class C325 { public int Value; }
        public sealed class C326 { public int Value; }
        public sealed class C327 { public int Value; }
        public sealed class C328 { public int Value; }
        public sealed class C329 { public int Value; }
        public sealed class C330 { public int Value; }
        public sealed class C331 { public int Value; }
        public sealed class C332 { public int Value; }
        public sealed class C333 { public int Value; }
        public sealed class C334 { public int Value; }
        public sealed class C335 { public int Value; }
        public sealed class C336 { public int Value; }
        public sealed class C337 { public int Value; }
        public sealed class C338 { public int Value; }
        public sealed class C339 { public int Value; }
        public sealed class C340 { public int Value; }
        public sealed class C341 { public int Value; }
        public sealed class C342 { public int Value; }
        public sealed class C343 { public int Value; }
        public sealed class C344 { public int Value; }
        public sealed class C345 { public int Value; }
        public sealed class C346 { public int Value; }
        public sealed class C347 { public int Value; }
        public sealed class C348 { public int Value; }
        public sealed class C349 { public int Value; }
        public sealed class C350 { public int Value; }
        public sealed class C351 { public int Value; }
        public sealed class C352 { public int Value; }
        public sealed class C353 { public int Value; }
        public sealed class C354 { public int Value; }
        public sealed class C355 { public int Value; }
        public sealed class C356 { public int Value; }
        public sealed class C357 { public int Value; }
        public sealed class C358 { public int Value; }
        public sealed class C359 { public int Value; }
        public sealed class C360 { public int Value; }
        public sealed class C361 { public int Value; }
        public sealed class C362 { public int Value; }
        public sealed class C363 { public int Value; }
        public sealed class C364 { public int Value; }
        public sealed class C365 { public int Value; }
        public sealed class C366 { public int Value; }
        public sealed class C367 { public int Value; }
        public sealed class C368 { public int Value; }
        public sealed class C369 { public int Value; }
        public sealed class C370 { public int Value; }
        public sealed class C371 { public int Value; }
        public sealed class C372 { public int Value; }
        public sealed class C373 { public int Value; }
        public sealed class C374 { public int Value; }
        public sealed class C375 { public int Value; }
        public sealed class C376 { public int Value; }
        public sealed class C377 { public int Value; }
        public sealed class C378 { public int Value; }
        public sealed class C379 { public int Value; }
        public sealed class C380 { public int Value; }
        public sealed class C381 { public int Value; }
        public sealed class C382 { public int Value; }
        public sealed class C383 { public int Value; }
        public sealed class C384 { public int Value; }
        public sealed class C385 { public int Value; }
        public sealed class C386 { public int Value; }
        public sealed class C387 { public int Value; }
        public sealed class C388 { public int Value; }
        public sealed class C389 { public int Value; }
        public sealed class C390 { public int Value; }
        public sealed class C391 { public int Value; }
        public sealed class C392 { public int Value; }
        public sealed class C393 { public int Value; }
        public sealed class C394 { public int Value; }
        public sealed class C395 { public int Value; }
        public sealed class C396 { public int Value; }
        public sealed class C397 { public int Value; }
        public sealed class C398 { public int Value; }
        public sealed class C399 { public int Value; }
        public sealed class C400 { public int Value; }
        public sealed class C401 { public int Value; }
        public sealed class C402 { public int Value; }
        public sealed class C403 { public int Value; }
        public sealed class C404 { public int Value; }
        public sealed class C405 { public int Value; }
        public sealed class C406 { public int Value; }
        public sealed class C407 { public int Value; }
        public sealed class C408 { public int Value; }
        public sealed class C409 { public int Value; }
        public sealed class C410 { public int Value; }
        public sealed class C411 { public int Value; }
        public sealed class C412 { public int Value; }
        public sealed class C413 { public int Value; }
        public sealed class C414 { public int Value; }
        public sealed class C415 { public int Value; }
        public sealed class C416 { public int Value; }
        public sealed class C417 { public int Value; }
        public sealed class C418 { public int Value; }
        public sealed class C419 { public int Value; }
        public sealed class C420 { public int Value; }
        public sealed class C421 { public int Value; }
        public sealed class C422 { public int Value; }
        public sealed class C423 { public int Value; }
        public sealed class C424 { public int Value; }
        public sealed class C425 { public int Value; }
        public sealed class C426 { public int Value; }
        public sealed class C427 { public int Value; }
        public sealed class C428 { public int Value; }
        public sealed class C429 { public int Value; }
        public sealed class C430 { public int Value; }
        public sealed class C431 { public int Value; }
        public sealed class C432 { public int Value; }
        public sealed class C433 { public int Value; }
        public sealed class C434 { public int Value; }
        public sealed class C435 { public int Value; }
        public sealed class C436 { public int Value; }
        public sealed class C437 { public int Value; }
        public sealed class C438 { public int Value; }
        public sealed class C439 { public int Value; }
        public sealed class C440 { public int Value; }
        public sealed class C441 { public int Value; }
        public sealed class C442 { public int Value; }
        public sealed class C443 { public int Value; }
        public sealed class C444 { public int Value; }
        public sealed class C445 { public int Value; }
        public sealed class C446 { public int Value; }
        public sealed class C447 { public int Value; }
        public sealed class C448 { public int Value; }
        public sealed class C449 { public int Value; }
        public sealed class C450 { public int Value; }
        public sealed class C451 { public int Value; }
        public sealed class C452 { public int Value; }
        public sealed class C453 { public int Value; }
        public sealed class C454 { public int Value; }
        public sealed class C455 { public int Value; }
        public sealed class C456 { public int Value; }
        public sealed class C457 { public int Value; }
        public sealed class C458 { public int Value; }
        public sealed class C459 { public int Value; }
        public sealed class C460 { public int Value; }
        public sealed class C461 { public int Value; }
        public sealed class C462 { public int Value; }
        public sealed class C463 { public int Value; }
        public sealed class C464 { public int Value; }
        public sealed class C465 { public int Value; }
        public sealed class C466 { public int Value; }
        public sealed class C467 { public int Value; }
        public sealed class C468 { public int Value; }
        public sealed class C469 { public int Value; }
        public sealed class C470 { public int Value; }
        public sealed class C471 { public int Value; }
        public sealed class C472 { public int Value; }
        public sealed class C473 { public int Value; }
        public sealed class C474 { public int Value; }
        public sealed class C475 { public int Value; }
        public sealed class C476 { public int Value; }
        public sealed class C477 { public int Value; }
        public sealed class C478 { public int Value; }
        public sealed class C479 { public int Value; }
        public sealed class C480 { public int Value; }
        public sealed class C481 { public int Value; }
        public sealed class C482 { public int Value; }
        public sealed class C483 { public int Value; }
        public sealed class C484 { public int Value; }
        public sealed class C485 { public int Value; }
        public sealed class C486 { public int Value; }
        public sealed class C487 { public int Value; }
        public sealed class C488 { public int Value; }
        public sealed class C489 { public int Value; }
        public sealed class C490 { public int Value; }
        public sealed class C491 { public int Value; }
        public sealed class C492 { public int Value; }
        public sealed class C493 { public int Value; }
        public sealed class C494 { public int Value; }
        public sealed class C495 { public int Value; }
        public sealed class C496 { public int Value; }
        public sealed class C497 { public int Value; }
        public sealed class C498 { public int Value; }
        public sealed class C499 { public int Value; }
        public sealed class C500 { public int Value; }
        public sealed class C501 { public int Value; }
        public sealed class C502 { public int Value; }
        public sealed class C503 { public int Value; }
        public sealed class C504 { public int Value; }
        public sealed class C505 { public int Value; }
        public sealed class C506 { public int Value; }
        public sealed class C507 { public int Value; }
        public sealed class C508 { public int Value; }
        public sealed class C509 { public int Value; }
        public sealed class C510 { public int Value; }
        public sealed class C511 { public int Value; }

        public static readonly Type[] Types = new Type[]
        {
            typeof(C0),
            typeof(C1),
            typeof(C2),
            typeof(C3),
            typeof(C4),
            typeof(C5),
            typeof(C6),
            typeof(C7),
            typeof(C8),
            typeof(C9),
            typeof(C10),
            typeof(C11),
            typeof(C12),
            typeof(C13),
            typeof(C14),
            typeof(C15),
            typeof(C16),
            typeof(C17),
            typeof(C18),
            typeof(C19),
            typeof(C20),
            typeof(C21),
            typeof(C22),
            typeof(C23),
            typeof(C24),
            typeof(C25),
            typeof(C26),
            typeof(C27),
            typeof(C28),
            typeof(C29),
            typeof(C30),
            typeof(C31),
            typeof(C32),
            typeof(C33),
            typeof(C34),
            typeof(C35),
            typeof(C36),
            typeof(C37),
            typeof(C38),
            typeof(C39),
            typeof(C40),
            typeof(C41),
            typeof(C42),
            typeof(C43),
            typeof(C44),
            typeof(C45),
            typeof(C46),
            typeof(C47),
            typeof(C48),
            typeof(C49),
            typeof(C50),
            typeof(C51),
            typeof(C52),
            typeof(C53),
            typeof(C54),
            typeof(C55),
            typeof(C56),
            typeof(C57),
            typeof(C58),
            typeof(C59),
            typeof(C60),
            typeof(C61),
            typeof(C62),
            typeof(C63),
            typeof(C64),
            typeof(C65),
            typeof(C66),
            typeof(C67),
            typeof(C68),
            typeof(C69),
            typeof(C70),
            typeof(C71),
            typeof(C72),
            typeof(C73),
            typeof(C74),
            typeof(C75),
            typeof(C76),
            typeof(C77),
            typeof(C78),
            typeof(C79),
            typeof(C80),
            typeof(C81),
            typeof(C82),
            typeof(C83),
            typeof(C84),
            typeof(C85),
            typeof(C86),
            typeof(C87),
            typeof(C88),
            typeof(C89),
            typeof(C90),
            typeof(C91),
            typeof(C92),
            typeof(C93),
            typeof(C94),
            typeof(C95),
            typeof(C96),
            typeof(C97),
            typeof(C98),
            typeof(C99),
            typeof(C100),
            typeof(C101),
            typeof(C102),
            typeof(C103),
            typeof(C104),
            typeof(C105),
            typeof(C106),
            typeof(C107),
            typeof(C108),
            typeof(C109),
            typeof(C110),
            typeof(C111),
            typeof(C112),
            typeof(C113),
            typeof(C114),
            typeof(C115),
            typeof(C116),
            typeof(C117),
            typeof(C118),
            typeof(C119),
            typeof(C120),
            typeof(C121),
            typeof(C122),
            typeof(C123),
            typeof(C124),
            typeof(C125),
            typeof(C126),
            typeof(C127),
            typeof(C128),
            typeof(C129),
            typeof(C130),
            typeof(C131),
            typeof(C132),
            typeof(C133),
            typeof(C134),
            typeof(C135),
            typeof(C136),
            typeof(C137),
            typeof(C138),
            typeof(C139),
            typeof(C140),
            typeof(C141),
            typeof(C142),
            typeof(C143),
            typeof(C144),
            typeof(C145),
            typeof(C146),
            typeof(C147),
            typeof(C148),
            typeof(C149),
            typeof(C150),
            typeof(C151),
            typeof(C152),
            typeof(C153),
            typeof(C154),
            typeof(C155),
            typeof(C156),
            typeof(C157),
            typeof(C158),
            typeof(C159),
            typeof(C160),
            typeof(C161),
            typeof(C162),
            typeof(C163),
            typeof(C164),
            typeof(C165),
            typeof(C166),
            typeof(C167),
            typeof(C168),
            typeof(C169),
            typeof(C170),
            typeof(C171),
            typeof(C172),
            typeof(C173),
            typeof(C174),
            typeof(C175),
            typeof(C176),
            typeof(C177),
            typeof(C178),
            typeof(C179),
            typeof(C180),
            typeof(C181),
            typeof(C182),
            typeof(C183),
            typeof(C184),
            typeof(C185),
            typeof(C186),
            typeof(C187),
            typeof(C188),
            typeof(C189),
            typeof(C190),
            typeof(C191),
            typeof(C192),
            typeof(C193),
            typeof(C194),
            typeof(C195),
            typeof(C196),
            typeof(C197),
            typeof(C198),
            typeof(C199),
            typeof(C200),
            typeof(C201),
            typeof(C202),
            typeof(C203),
            typeof(C204),
            typeof(C205),
            typeof(C206),
            typeof(C207),
            typeof(C208),
            typeof(C209),
            typeof(C210),
            typeof(C211),
            typeof(C212),
            typeof(C213),
            typeof(C214),
            typeof(C215),
            typeof(C216),
            typeof(C217),
            typeof(C218),
            typeof(C219),
            typeof(C220),
            typeof(C221),
            typeof(C222),
            typeof(C223),
            typeof(C224),
            typeof(C225),
            typeof(C226),
            typeof(C227),
            typeof(C228),
            typeof(C229),
            typeof(C230),
            typeof(C231),
            typeof(C232),
            typeof(C233),
            typeof(C234),
            typeof(C235),
            typeof(C236),
            typeof(C237),
            typeof(C238),
            typeof(C239),
            typeof(C240),
            typeof(C241),
            typeof(C242),
            typeof(C243),
            typeof(C244),
            typeof(C245),
            typeof(C246),
            typeof(C247),
            typeof(C248),
            typeof(C249),
            typeof(C250),
            typeof(C251),
            typeof(C252),
            typeof(C253),
            typeof(C254),
            typeof(C255),
            typeof(C256),
            typeof(C257),
            typeof(C258),
            typeof(C259),
            typeof(C260),
            typeof(C261),
            typeof(C262),
            typeof(C263),
            typeof(C264),
            typeof(C265),
            typeof(C266),
            typeof(C267),
            typeof(C268),
            typeof(C269),
            typeof(C270),
            typeof(C271),
            typeof(C272),
            typeof(C273),
            typeof(C274),
            typeof(C275),
            typeof(C276),
            typeof(C277),
            typeof(C278),
            typeof(C279),
            typeof(C280),
            typeof(C281),
            typeof(C282),
            typeof(C283),
            typeof(C284),
            typeof(C285),
            typeof(C286),
            typeof(C287),
            typeof(C288),
            typeof(C289),
            typeof(C290),
            typeof(C291),
            typeof(C292),
            typeof(C293),
            typeof(C294),
            typeof(C295),
            typeof(C296),
            typeof(C297),
            typeof(C298),
            typeof(C299),
            typeof(C300),
            typeof(C301),
            typeof(C302),
            typeof(C303),
            typeof(C304),
            typeof(C305),
            typeof(C306),
            typeof(C307),
            typeof(C308),
            typeof(C309),
            typeof(C310),
            typeof(C311),
            typeof(C312),
            typeof(C313),
            typeof(C314),
            typeof(C315),
            typeof(C316),
            typeof(C317),
            typeof(C318),
            typeof(C319),
            typeof(C320),
            typeof(C321),
            typeof(C322),
            typeof(C323),
            typeof(C324),
            typeof(C325),
            typeof(C326),
            typeof(C327),
            typeof(C328),
            typeof(C329),
            typeof(C330),
            typeof(C331),
            typeof(C332),
            typeof(C333),
            typeof(C334),
            typeof(C335),
            typeof(C336),
            typeof(C337),
            typeof(C338),
            typeof(C339),
            typeof(C340),
            typeof(C341),
            typeof(C342),
            typeof(C343),
            typeof(C344),
            typeof(C345),
            typeof(C346),
            typeof(C347),
            typeof(C348),
            typeof(C349),
            typeof(C350),
            typeof(C351),
            typeof(C352),
            typeof(C353),
            typeof(C354),
            typeof(C355),
            typeof(C356),
            typeof(C357),
            typeof(C358),
            typeof(C359),
            typeof(C360),
            typeof(C361),
            typeof(C362),
            typeof(C363),
            typeof(C364),
            typeof(C365),
            typeof(C366),
            typeof(C367),
            typeof(C368),
            typeof(C369),
            typeof(C370),
            typeof(C371),
            typeof(C372),
            typeof(C373),
            typeof(C374),
            typeof(C375),
            typeof(C376),
            typeof(C377),
            typeof(C378),
            typeof(C379),
            typeof(C380),
            typeof(C381),
            typeof(C382),
            typeof(C383),
            typeof(C384),
            typeof(C385),
            typeof(C386),
            typeof(C387),
            typeof(C388),
            typeof(C389),
            typeof(C390),
            typeof(C391),
            typeof(C392),
            typeof(C393),
            typeof(C394),
            typeof(C395),
            typeof(C396),
            typeof(C397),
            typeof(C398),
            typeof(C399),
            typeof(C400),
            typeof(C401),
            typeof(C402),
            typeof(C403),
            typeof(C404),
            typeof(C405),
            typeof(C406),
            typeof(C407),
            typeof(C408),
            typeof(C409),
            typeof(C410),
            typeof(C411),
            typeof(C412),
            typeof(C413),
            typeof(C414),
            typeof(C415),
            typeof(C416),
            typeof(C417),
            typeof(C418),
            typeof(C419),
            typeof(C420),
            typeof(C421),
            typeof(C422),
            typeof(C423),
            typeof(C424),
            typeof(C425),
            typeof(C426),
            typeof(C427),
            typeof(C428),
            typeof(C429),
            typeof(C430),
            typeof(C431),
            typeof(C432),
            typeof(C433),
            typeof(C434),
            typeof(C435),
            typeof(C436),
            typeof(C437),
            typeof(C438),
            typeof(C439),
            typeof(C440),
            typeof(C441),
            typeof(C442),
            typeof(C443),
            typeof(C444),
            typeof(C445),
            typeof(C446),
            typeof(C447),
            typeof(C448),
            typeof(C449),
            typeof(C450),
            typeof(C451),
            typeof(C452),
            typeof(C453),
            typeof(C454),
            typeof(C455),
            typeof(C456),
            typeof(C457),
            typeof(C458),
            typeof(C459),
            typeof(C460),
            typeof(C461),
            typeof(C462),
            typeof(C463),
            typeof(C464),
            typeof(C465),
            typeof(C466),
            typeof(C467),
            typeof(C468),
            typeof(C469),
            typeof(C470),
            typeof(C471),
            typeof(C472),
            typeof(C473),
            typeof(C474),
            typeof(C475),
            typeof(C476),
            typeof(C477),
            typeof(C478),
            typeof(C479),
            typeof(C480),
            typeof(C481),
            typeof(C482),
            typeof(C483),
            typeof(C484),
            typeof(C485),
            typeof(C486),
            typeof(C487),
            typeof(C488),
            typeof(C489),
            typeof(C490),
            typeof(C491),
            typeof(C492),
            typeof(C493),
            typeof(C494),
            typeof(C495),
            typeof(C496),
            typeof(C497),
            typeof(C498),
            typeof(C499),
            typeof(C500),
            typeof(C501),
            typeof(C502),
            typeof(C503),
            typeof(C504),
            typeof(C505),
            typeof(C506),
            typeof(C507),
            typeof(C508),
            typeof(C509),
            typeof(C510),
            typeof(C511),
        };

        public static readonly object[] Instances = new object[]
        {
            new C0(),
            new C1(),
            new C2(),
            new C3(),
            new C4(),
            new C5(),
            new C6(),
            new C7(),
            new C8(),
            new C9(),
            new C10(),
            new C11(),
            new C12(),
            new C13(),
            new C14(),
            new C15(),
            new C16(),
            new C17(),
            new C18(),
            new C19(),
            new C20(),
            new C21(),
            new C22(),
            new C23(),
            new C24(),
            new C25(),
            new C26(),
            new C27(),
            new C28(),
            new C29(),
            new C30(),
            new C31(),
            new C32(),
            new C33(),
            new C34(),
            new C35(),
            new C36(),
            new C37(),
            new C38(),
            new C39(),
            new C40(),
            new C41(),
            new C42(),
            new C43(),
            new C44(),
            new C45(),
            new C46(),
            new C47(),
            new C48(),
            new C49(),
            new C50(),
            new C51(),
            new C52(),
            new C53(),
            new C54(),
            new C55(),
            new C56(),
            new C57(),
            new C58(),
            new C59(),
            new C60(),
            new C61(),
            new C62(),
            new C63(),
            new C64(),
            new C65(),
            new C66(),
            new C67(),
            new C68(),
            new C69(),
            new C70(),
            new C71(),
            new C72(),
            new C73(),
            new C74(),
            new C75(),
            new C76(),
            new C77(),
            new C78(),
            new C79(),
            new C80(),
            new C81(),
            new C82(),
            new C83(),
            new C84(),
            new C85(),
            new C86(),
            new C87(),
            new C88(),
            new C89(),
            new C90(),
            new C91(),
            new C92(),
            new C93(),
            new C94(),
            new C95(),
            new C96(),
            new C97(),
            new C98(),
            new C99(),
            new C100(),
            new C101(),
            new C102(),
            new C103(),
            new C104(),
            new C105(),
            new C106(),
            new C107(),
            new C108(),
            new C109(),
            new C110(),
            new C111(),
            new C112(),
            new C113(),
            new C114(),
            new C115(),
            new C116(),
            new C117(),
            new C118(),
            new C119(),
            new C120(),
            new C121(),
            new C122(),
            new C123(),
            new C124(),
            new C125(),
            new C126(),
            new C127(),
            new C128(),
            new C129(),
            new C130(),
            new C131(),
            new C132(),
            new C133(),
            new C134(),
            new C135(),
            new C136(),
            new C137(),
            new C138(),
            new C139(),
            new C140(),
            new C141(),
            new C142(),
            new C143(),
            new C144(),
            new C145(),
            new C146(),
            new C147(),
            new C148(),
            new C149(),
            new C150(),
            new C151(),
            new C152(),
            new C153(),
            new C154(),
            new C155(),
            new C156(),
            new C157(),
            new C158(),
            new C159(),
            new C160(),
            new C161(),
            new C162(),
            new C163(),
            new C164(),
            new C165(),
            new C166(),
            new C167(),
            new C168(),
            new C169(),
            new C170(),
            new C171(),
            new C172(),
            new C173(),
            new C174(),
            new C175(),
            new C176(),
            new C177(),
            new C178(),
            new C179(),
            new C180(),
            new C181(),
            new C182(),
            new C183(),
            new C184(),
            new C185(),
            new C186(),
            new C187(),
            new C188(),
            new C189(),
            new C190(),
            new C191(),
            new C192(),
            new C193(),
            new C194(),
            new C195(),
            new C196(),
            new C197(),
            new C198(),
            new C199(),
            new C200(),
            new C201(),
            new C202(),
            new C203(),
            new C204(),
            new C205(),
            new C206(),
            new C207(),
            new C208(),
            new C209(),
            new C210(),
            new C211(),
            new C212(),
            new C213(),
            new C214(),
            new C215(),
            new C216(),
            new C217(),
            new C218(),
            new C219(),
            new C220(),
            new C221(),
            new C222(),
            new C223(),
            new C224(),
            new C225(),
            new C226(),
            new C227(),
            new C228(),
            new C229(),
            new C230(),
            new C231(),
            new C232(),
            new C233(),
            new C234(),
            new C235(),
            new C236(),
            new C237(),
            new C238(),
            new C239(),
            new C240(),
            new C241(),
            new C242(),
            new C243(),
            new C244(),
            new C245(),
            new C246(),
            new C247(),
            new C248(),
            new C249(),
            new C250(),
            new C251(),
            new C252(),
            new C253(),
            new C254(),
            new C255(),
            new C256(),
            new C257(),
            new C258(),
            new C259(),
            new C260(),
            new C261(),
            new C262(),
            new C263(),
            new C264(),
            new C265(),
            new C266(),
            new C267(),
            new C268(),
            new C269(),
            new C270(),
            new C271(),
            new C272(),
            new C273(),
            new C274(),
            new C275(),
            new C276(),
            new C277(),
            new C278(),
            new C279(),
            new C280(),
            new C281(),
            new C282(),
            new C283(),
            new C284(),
            new C285(),
            new C286(),
            new C287(),
            new C288(),
            new C289(),
            new C290(),
            new C291(),
            new C292(),
            new C293(),
            new C294(),
            new C295(),
            new C296(),
            new C297(),
            new C298(),
            new C299(),
            new C300(),
            new C301(),
            new C302(),
            new C303(),
            new C304(),
            new C305(),
            new C306(),
            new C307(),
            new C308(),
            new C309(),
            new C310(),
            new C311(),
            new C312(),
            new C313(),
            new C314(),
            new C315(),
            new C316(),
            new C317(),
            new C318(),
            new C319(),
            new C320(),
            new C321(),
            new C322(),
            new C323(),
            new C324(),
            new C325(),
            new C326(),
            new C327(),
            new C328(),
            new C329(),
            new C330(),
            new C331(),
            new C332(),
            new C333(),
            new C334(),
            new C335(),
            new C336(),
            new C337(),
            new C338(),
            new C339(),
            new C340(),
            new C341(),
            new C342(),
            new C343(),
            new C344(),
            new C345(),
            new C346(),
            new C347(),
            new C348(),
            new C349(),
            new C350(),
            new C351(),
            new C352(),
            new C353(),
            new C354(),
            new C355(),
            new C356(),
            new C357(),
            new C358(),
            new C359(),
            new C360(),
            new C361(),
            new C362(),
            new C363(),
            new C364(),
            new C365(),
            new C366(),
            new C367(),
            new C368(),
            new C369(),
            new C370(),
            new C371(),
            new C372(),
            new C373(),
            new C374(),
            new C375(),
            new C376(),
            new C377(),
            new C378(),
            new C379(),
            new C380(),
            new C381(),
            new C382(),
            new C383(),
            new C384(),
            new C385(),
            new C386(),
            new C387(),
            new C388(),
            new C389(),
            new C390(),
            new C391(),
            new C392(),
            new C393(),
            new C394(),
            new C395(),
            new C396(),
            new C397(),
            new C398(),
            new C399(),
            new C400(),
            new C401(),
            new C402(),
            new C403(),
            new C404(),
            new C405(),
            new C406(),
            new C407(),
            new C408(),
            new C409(),
            new C410(),
            new C411(),
            new C412(),
            new C413(),
            new C414(),
            new C415(),
            new C416(),
            new C417(),
            new C418(),
            new C419(),
            new C420(),
            new C421(),
            new C422(),
            new C423(),
            new C424(),
            new C425(),
            new C426(),
            new C427(),
            new C428(),
            new C429(),
            new C430(),
            new C431(),
            new C432(),
            new C433(),
            new C434(),
            new C435(),
            new C436(),
            new C437(),
            new C438(),
            new C439(),
            new C440(),
            new C441(),
            new C442(),
            new C443(),
            new C444(),
            new C445(),
            new C446(),
            new C447(),
            new C448(),
            new C449(),
            new C450(),
            new C451(),
            new C452(),
            new C453(),
            new C454(),
            new C455(),
            new C456(),
            new C457(),
            new C458(),
            new C459(),
            new C460(),
            new C461(),
            new C462(),
            new C463(),
            new C464(),
            new C465(),
            new C466(),
            new C467(),
            new C468(),
            new C469(),
            new C470(),
            new C471(),
            new C472(),
            new C473(),
            new C474(),
            new C475(),
            new C476(),
            new C477(),
            new C478(),
            new C479(),
            new C480(),
            new C481(),
            new C482(),
            new C483(),
            new C484(),
            new C485(),
            new C486(),
            new C487(),
            new C488(),
            new C489(),
            new C490(),
            new C491(),
            new C492(),
            new C493(),
            new C494(),
            new C495(),
            new C496(),
            new C497(),
            new C498(),
            new C499(),
            new C500(),
            new C501(),
            new C502(),
            new C503(),
            new C504(),
            new C505(),
            new C506(),
            new C507(),
            new C508(),
            new C509(),
            new C510(),
            new C511(),
        };

        public static int IfChainObject8(object value)
        {
            if (value is C0) return 0;
            if (value is C1) return 1;
            if (value is C2) return 2;
            if (value is C3) return 3;
            if (value is C4) return 4;
            if (value is C5) return 5;
            if (value is C6) return 6;
            if (value is C7) return 7;
            return -1;
        }

        public static int IfChainObject64(object value)
        {
            if (value is C0) return 0;
            if (value is C1) return 1;
            if (value is C2) return 2;
            if (value is C3) return 3;
            if (value is C4) return 4;
            if (value is C5) return 5;
            if (value is C6) return 6;
            if (value is C7) return 7;
            if (value is C8) return 8;
            if (value is C9) return 9;
            if (value is C10) return 10;
            if (value is C11) return 11;
            if (value is C12) return 12;
            if (value is C13) return 13;
            if (value is C14) return 14;
            if (value is C15) return 15;
            if (value is C16) return 16;
            if (value is C17) return 17;
            if (value is C18) return 18;
            if (value is C19) return 19;
            if (value is C20) return 20;
            if (value is C21) return 21;
            if (value is C22) return 22;
            if (value is C23) return 23;
            if (value is C24) return 24;
            if (value is C25) return 25;
            if (value is C26) return 26;
            if (value is C27) return 27;
            if (value is C28) return 28;
            if (value is C29) return 29;
            if (value is C30) return 30;
            if (value is C31) return 31;
            if (value is C32) return 32;
            if (value is C33) return 33;
            if (value is C34) return 34;
            if (value is C35) return 35;
            if (value is C36) return 36;
            if (value is C37) return 37;
            if (value is C38) return 38;
            if (value is C39) return 39;
            if (value is C40) return 40;
            if (value is C41) return 41;
            if (value is C42) return 42;
            if (value is C43) return 43;
            if (value is C44) return 44;
            if (value is C45) return 45;
            if (value is C46) return 46;
            if (value is C47) return 47;
            if (value is C48) return 48;
            if (value is C49) return 49;
            if (value is C50) return 50;
            if (value is C51) return 51;
            if (value is C52) return 52;
            if (value is C53) return 53;
            if (value is C54) return 54;
            if (value is C55) return 55;
            if (value is C56) return 56;
            if (value is C57) return 57;
            if (value is C58) return 58;
            if (value is C59) return 59;
            if (value is C60) return 60;
            if (value is C61) return 61;
            if (value is C62) return 62;
            if (value is C63) return 63;
            return -1;
        }

        public static int IfChainObject512(object value)
        {
            if (value is C0) return 0;
            if (value is C1) return 1;
            if (value is C2) return 2;
            if (value is C3) return 3;
            if (value is C4) return 4;
            if (value is C5) return 5;
            if (value is C6) return 6;
            if (value is C7) return 7;
            if (value is C8) return 8;
            if (value is C9) return 9;
            if (value is C10) return 10;
            if (value is C11) return 11;
            if (value is C12) return 12;
            if (value is C13) return 13;
            if (value is C14) return 14;
            if (value is C15) return 15;
            if (value is C16) return 16;
            if (value is C17) return 17;
            if (value is C18) return 18;
            if (value is C19) return 19;
            if (value is C20) return 20;
            if (value is C21) return 21;
            if (value is C22) return 22;
            if (value is C23) return 23;
            if (value is C24) return 24;
            if (value is C25) return 25;
            if (value is C26) return 26;
            if (value is C27) return 27;
            if (value is C28) return 28;
            if (value is C29) return 29;
            if (value is C30) return 30;
            if (value is C31) return 31;
            if (value is C32) return 32;
            if (value is C33) return 33;
            if (value is C34) return 34;
            if (value is C35) return 35;
            if (value is C36) return 36;
            if (value is C37) return 37;
            if (value is C38) return 38;
            if (value is C39) return 39;
            if (value is C40) return 40;
            if (value is C41) return 41;
            if (value is C42) return 42;
            if (value is C43) return 43;
            if (value is C44) return 44;
            if (value is C45) return 45;
            if (value is C46) return 46;
            if (value is C47) return 47;
            if (value is C48) return 48;
            if (value is C49) return 49;
            if (value is C50) return 50;
            if (value is C51) return 51;
            if (value is C52) return 52;
            if (value is C53) return 53;
            if (value is C54) return 54;
            if (value is C55) return 55;
            if (value is C56) return 56;
            if (value is C57) return 57;
            if (value is C58) return 58;
            if (value is C59) return 59;
            if (value is C60) return 60;
            if (value is C61) return 61;
            if (value is C62) return 62;
            if (value is C63) return 63;
            if (value is C64) return 64;
            if (value is C65) return 65;
            if (value is C66) return 66;
            if (value is C67) return 67;
            if (value is C68) return 68;
            if (value is C69) return 69;
            if (value is C70) return 70;
            if (value is C71) return 71;
            if (value is C72) return 72;
            if (value is C73) return 73;
            if (value is C74) return 74;
            if (value is C75) return 75;
            if (value is C76) return 76;
            if (value is C77) return 77;
            if (value is C78) return 78;
            if (value is C79) return 79;
            if (value is C80) return 80;
            if (value is C81) return 81;
            if (value is C82) return 82;
            if (value is C83) return 83;
            if (value is C84) return 84;
            if (value is C85) return 85;
            if (value is C86) return 86;
            if (value is C87) return 87;
            if (value is C88) return 88;
            if (value is C89) return 89;
            if (value is C90) return 90;
            if (value is C91) return 91;
            if (value is C92) return 92;
            if (value is C93) return 93;
            if (value is C94) return 94;
            if (value is C95) return 95;
            if (value is C96) return 96;
            if (value is C97) return 97;
            if (value is C98) return 98;
            if (value is C99) return 99;
            if (value is C100) return 100;
            if (value is C101) return 101;
            if (value is C102) return 102;
            if (value is C103) return 103;
            if (value is C104) return 104;
            if (value is C105) return 105;
            if (value is C106) return 106;
            if (value is C107) return 107;
            if (value is C108) return 108;
            if (value is C109) return 109;
            if (value is C110) return 110;
            if (value is C111) return 111;
            if (value is C112) return 112;
            if (value is C113) return 113;
            if (value is C114) return 114;
            if (value is C115) return 115;
            if (value is C116) return 116;
            if (value is C117) return 117;
            if (value is C118) return 118;
            if (value is C119) return 119;
            if (value is C120) return 120;
            if (value is C121) return 121;
            if (value is C122) return 122;
            if (value is C123) return 123;
            if (value is C124) return 124;
            if (value is C125) return 125;
            if (value is C126) return 126;
            if (value is C127) return 127;
            if (value is C128) return 128;
            if (value is C129) return 129;
            if (value is C130) return 130;
            if (value is C131) return 131;
            if (value is C132) return 132;
            if (value is C133) return 133;
            if (value is C134) return 134;
            if (value is C135) return 135;
            if (value is C136) return 136;
            if (value is C137) return 137;
            if (value is C138) return 138;
            if (value is C139) return 139;
            if (value is C140) return 140;
            if (value is C141) return 141;
            if (value is C142) return 142;
            if (value is C143) return 143;
            if (value is C144) return 144;
            if (value is C145) return 145;
            if (value is C146) return 146;
            if (value is C147) return 147;
            if (value is C148) return 148;
            if (value is C149) return 149;
            if (value is C150) return 150;
            if (value is C151) return 151;
            if (value is C152) return 152;
            if (value is C153) return 153;
            if (value is C154) return 154;
            if (value is C155) return 155;
            if (value is C156) return 156;
            if (value is C157) return 157;
            if (value is C158) return 158;
            if (value is C159) return 159;
            if (value is C160) return 160;
            if (value is C161) return 161;
            if (value is C162) return 162;
            if (value is C163) return 163;
            if (value is C164) return 164;
            if (value is C165) return 165;
            if (value is C166) return 166;
            if (value is C167) return 167;
            if (value is C168) return 168;
            if (value is C169) return 169;
            if (value is C170) return 170;
            if (value is C171) return 171;
            if (value is C172) return 172;
            if (value is C173) return 173;
            if (value is C174) return 174;
            if (value is C175) return 175;
            if (value is C176) return 176;
            if (value is C177) return 177;
            if (value is C178) return 178;
            if (value is C179) return 179;
            if (value is C180) return 180;
            if (value is C181) return 181;
            if (value is C182) return 182;
            if (value is C183) return 183;
            if (value is C184) return 184;
            if (value is C185) return 185;
            if (value is C186) return 186;
            if (value is C187) return 187;
            if (value is C188) return 188;
            if (value is C189) return 189;
            if (value is C190) return 190;
            if (value is C191) return 191;
            if (value is C192) return 192;
            if (value is C193) return 193;
            if (value is C194) return 194;
            if (value is C195) return 195;
            if (value is C196) return 196;
            if (value is C197) return 197;
            if (value is C198) return 198;
            if (value is C199) return 199;
            if (value is C200) return 200;
            if (value is C201) return 201;
            if (value is C202) return 202;
            if (value is C203) return 203;
            if (value is C204) return 204;
            if (value is C205) return 205;
            if (value is C206) return 206;
            if (value is C207) return 207;
            if (value is C208) return 208;
            if (value is C209) return 209;
            if (value is C210) return 210;
            if (value is C211) return 211;
            if (value is C212) return 212;
            if (value is C213) return 213;
            if (value is C214) return 214;
            if (value is C215) return 215;
            if (value is C216) return 216;
            if (value is C217) return 217;
            if (value is C218) return 218;
            if (value is C219) return 219;
            if (value is C220) return 220;
            if (value is C221) return 221;
            if (value is C222) return 222;
            if (value is C223) return 223;
            if (value is C224) return 224;
            if (value is C225) return 225;
            if (value is C226) return 226;
            if (value is C227) return 227;
            if (value is C228) return 228;
            if (value is C229) return 229;
            if (value is C230) return 230;
            if (value is C231) return 231;
            if (value is C232) return 232;
            if (value is C233) return 233;
            if (value is C234) return 234;
            if (value is C235) return 235;
            if (value is C236) return 236;
            if (value is C237) return 237;
            if (value is C238) return 238;
            if (value is C239) return 239;
            if (value is C240) return 240;
            if (value is C241) return 241;
            if (value is C242) return 242;
            if (value is C243) return 243;
            if (value is C244) return 244;
            if (value is C245) return 245;
            if (value is C246) return 246;
            if (value is C247) return 247;
            if (value is C248) return 248;
            if (value is C249) return 249;
            if (value is C250) return 250;
            if (value is C251) return 251;
            if (value is C252) return 252;
            if (value is C253) return 253;
            if (value is C254) return 254;
            if (value is C255) return 255;
            if (value is C256) return 256;
            if (value is C257) return 257;
            if (value is C258) return 258;
            if (value is C259) return 259;
            if (value is C260) return 260;
            if (value is C261) return 261;
            if (value is C262) return 262;
            if (value is C263) return 263;
            if (value is C264) return 264;
            if (value is C265) return 265;
            if (value is C266) return 266;
            if (value is C267) return 267;
            if (value is C268) return 268;
            if (value is C269) return 269;
            if (value is C270) return 270;
            if (value is C271) return 271;
            if (value is C272) return 272;
            if (value is C273) return 273;
            if (value is C274) return 274;
            if (value is C275) return 275;
            if (value is C276) return 276;
            if (value is C277) return 277;
            if (value is C278) return 278;
            if (value is C279) return 279;
            if (value is C280) return 280;
            if (value is C281) return 281;
            if (value is C282) return 282;
            if (value is C283) return 283;
            if (value is C284) return 284;
            if (value is C285) return 285;
            if (value is C286) return 286;
            if (value is C287) return 287;
            if (value is C288) return 288;
            if (value is C289) return 289;
            if (value is C290) return 290;
            if (value is C291) return 291;
            if (value is C292) return 292;
            if (value is C293) return 293;
            if (value is C294) return 294;
            if (value is C295) return 295;
            if (value is C296) return 296;
            if (value is C297) return 297;
            if (value is C298) return 298;
            if (value is C299) return 299;
            if (value is C300) return 300;
            if (value is C301) return 301;
            if (value is C302) return 302;
            if (value is C303) return 303;
            if (value is C304) return 304;
            if (value is C305) return 305;
            if (value is C306) return 306;
            if (value is C307) return 307;
            if (value is C308) return 308;
            if (value is C309) return 309;
            if (value is C310) return 310;
            if (value is C311) return 311;
            if (value is C312) return 312;
            if (value is C313) return 313;
            if (value is C314) return 314;
            if (value is C315) return 315;
            if (value is C316) return 316;
            if (value is C317) return 317;
            if (value is C318) return 318;
            if (value is C319) return 319;
            if (value is C320) return 320;
            if (value is C321) return 321;
            if (value is C322) return 322;
            if (value is C323) return 323;
            if (value is C324) return 324;
            if (value is C325) return 325;
            if (value is C326) return 326;
            if (value is C327) return 327;
            if (value is C328) return 328;
            if (value is C329) return 329;
            if (value is C330) return 330;
            if (value is C331) return 331;
            if (value is C332) return 332;
            if (value is C333) return 333;
            if (value is C334) return 334;
            if (value is C335) return 335;
            if (value is C336) return 336;
            if (value is C337) return 337;
            if (value is C338) return 338;
            if (value is C339) return 339;
            if (value is C340) return 340;
            if (value is C341) return 341;
            if (value is C342) return 342;
            if (value is C343) return 343;
            if (value is C344) return 344;
            if (value is C345) return 345;
            if (value is C346) return 346;
            if (value is C347) return 347;
            if (value is C348) return 348;
            if (value is C349) return 349;
            if (value is C350) return 350;
            if (value is C351) return 351;
            if (value is C352) return 352;
            if (value is C353) return 353;
            if (value is C354) return 354;
            if (value is C355) return 355;
            if (value is C356) return 356;
            if (value is C357) return 357;
            if (value is C358) return 358;
            if (value is C359) return 359;
            if (value is C360) return 360;
            if (value is C361) return 361;
            if (value is C362) return 362;
            if (value is C363) return 363;
            if (value is C364) return 364;
            if (value is C365) return 365;
            if (value is C366) return 366;
            if (value is C367) return 367;
            if (value is C368) return 368;
            if (value is C369) return 369;
            if (value is C370) return 370;
            if (value is C371) return 371;
            if (value is C372) return 372;
            if (value is C373) return 373;
            if (value is C374) return 374;
            if (value is C375) return 375;
            if (value is C376) return 376;
            if (value is C377) return 377;
            if (value is C378) return 378;
            if (value is C379) return 379;
            if (value is C380) return 380;
            if (value is C381) return 381;
            if (value is C382) return 382;
            if (value is C383) return 383;
            if (value is C384) return 384;
            if (value is C385) return 385;
            if (value is C386) return 386;
            if (value is C387) return 387;
            if (value is C388) return 388;
            if (value is C389) return 389;
            if (value is C390) return 390;
            if (value is C391) return 391;
            if (value is C392) return 392;
            if (value is C393) return 393;
            if (value is C394) return 394;
            if (value is C395) return 395;
            if (value is C396) return 396;
            if (value is C397) return 397;
            if (value is C398) return 398;
            if (value is C399) return 399;
            if (value is C400) return 400;
            if (value is C401) return 401;
            if (value is C402) return 402;
            if (value is C403) return 403;
            if (value is C404) return 404;
            if (value is C405) return 405;
            if (value is C406) return 406;
            if (value is C407) return 407;
            if (value is C408) return 408;
            if (value is C409) return 409;
            if (value is C410) return 410;
            if (value is C411) return 411;
            if (value is C412) return 412;
            if (value is C413) return 413;
            if (value is C414) return 414;
            if (value is C415) return 415;
            if (value is C416) return 416;
            if (value is C417) return 417;
            if (value is C418) return 418;
            if (value is C419) return 419;
            if (value is C420) return 420;
            if (value is C421) return 421;
            if (value is C422) return 422;
            if (value is C423) return 423;
            if (value is C424) return 424;
            if (value is C425) return 425;
            if (value is C426) return 426;
            if (value is C427) return 427;
            if (value is C428) return 428;
            if (value is C429) return 429;
            if (value is C430) return 430;
            if (value is C431) return 431;
            if (value is C432) return 432;
            if (value is C433) return 433;
            if (value is C434) return 434;
            if (value is C435) return 435;
            if (value is C436) return 436;
            if (value is C437) return 437;
            if (value is C438) return 438;
            if (value is C439) return 439;
            if (value is C440) return 440;
            if (value is C441) return 441;
            if (value is C442) return 442;
            if (value is C443) return 443;
            if (value is C444) return 444;
            if (value is C445) return 445;
            if (value is C446) return 446;
            if (value is C447) return 447;
            if (value is C448) return 448;
            if (value is C449) return 449;
            if (value is C450) return 450;
            if (value is C451) return 451;
            if (value is C452) return 452;
            if (value is C453) return 453;
            if (value is C454) return 454;
            if (value is C455) return 455;
            if (value is C456) return 456;
            if (value is C457) return 457;
            if (value is C458) return 458;
            if (value is C459) return 459;
            if (value is C460) return 460;
            if (value is C461) return 461;
            if (value is C462) return 462;
            if (value is C463) return 463;
            if (value is C464) return 464;
            if (value is C465) return 465;
            if (value is C466) return 466;
            if (value is C467) return 467;
            if (value is C468) return 468;
            if (value is C469) return 469;
            if (value is C470) return 470;
            if (value is C471) return 471;
            if (value is C472) return 472;
            if (value is C473) return 473;
            if (value is C474) return 474;
            if (value is C475) return 475;
            if (value is C476) return 476;
            if (value is C477) return 477;
            if (value is C478) return 478;
            if (value is C479) return 479;
            if (value is C480) return 480;
            if (value is C481) return 481;
            if (value is C482) return 482;
            if (value is C483) return 483;
            if (value is C484) return 484;
            if (value is C485) return 485;
            if (value is C486) return 486;
            if (value is C487) return 487;
            if (value is C488) return 488;
            if (value is C489) return 489;
            if (value is C490) return 490;
            if (value is C491) return 491;
            if (value is C492) return 492;
            if (value is C493) return 493;
            if (value is C494) return 494;
            if (value is C495) return 495;
            if (value is C496) return 496;
            if (value is C497) return 497;
            if (value is C498) return 498;
            if (value is C499) return 499;
            if (value is C500) return 500;
            if (value is C501) return 501;
            if (value is C502) return 502;
            if (value is C503) return 503;
            if (value is C504) return 504;
            if (value is C505) return 505;
            if (value is C506) return 506;
            if (value is C507) return 507;
            if (value is C508) return 508;
            if (value is C509) return 509;
            if (value is C510) return 510;
            if (value is C511) return 511;
            return -1;
        }

        public static int IfChainType8(Type type)
        {
            if (type == typeof(C0)) return 0;
            if (type == typeof(C1)) return 1;
            if (type == typeof(C2)) return 2;
            if (type == typeof(C3)) return 3;
            if (type == typeof(C4)) return 4;
            if (type == typeof(C5)) return 5;
            if (type == typeof(C6)) return 6;
            if (type == typeof(C7)) return 7;
            return -1;
        }

        public static int IfChainType64(Type type)
        {
            if (type == typeof(C0)) return 0;
            if (type == typeof(C1)) return 1;
            if (type == typeof(C2)) return 2;
            if (type == typeof(C3)) return 3;
            if (type == typeof(C4)) return 4;
            if (type == typeof(C5)) return 5;
            if (type == typeof(C6)) return 6;
            if (type == typeof(C7)) return 7;
            if (type == typeof(C8)) return 8;
            if (type == typeof(C9)) return 9;
            if (type == typeof(C10)) return 10;
            if (type == typeof(C11)) return 11;
            if (type == typeof(C12)) return 12;
            if (type == typeof(C13)) return 13;
            if (type == typeof(C14)) return 14;
            if (type == typeof(C15)) return 15;
            if (type == typeof(C16)) return 16;
            if (type == typeof(C17)) return 17;
            if (type == typeof(C18)) return 18;
            if (type == typeof(C19)) return 19;
            if (type == typeof(C20)) return 20;
            if (type == typeof(C21)) return 21;
            if (type == typeof(C22)) return 22;
            if (type == typeof(C23)) return 23;
            if (type == typeof(C24)) return 24;
            if (type == typeof(C25)) return 25;
            if (type == typeof(C26)) return 26;
            if (type == typeof(C27)) return 27;
            if (type == typeof(C28)) return 28;
            if (type == typeof(C29)) return 29;
            if (type == typeof(C30)) return 30;
            if (type == typeof(C31)) return 31;
            if (type == typeof(C32)) return 32;
            if (type == typeof(C33)) return 33;
            if (type == typeof(C34)) return 34;
            if (type == typeof(C35)) return 35;
            if (type == typeof(C36)) return 36;
            if (type == typeof(C37)) return 37;
            if (type == typeof(C38)) return 38;
            if (type == typeof(C39)) return 39;
            if (type == typeof(C40)) return 40;
            if (type == typeof(C41)) return 41;
            if (type == typeof(C42)) return 42;
            if (type == typeof(C43)) return 43;
            if (type == typeof(C44)) return 44;
            if (type == typeof(C45)) return 45;
            if (type == typeof(C46)) return 46;
            if (type == typeof(C47)) return 47;
            if (type == typeof(C48)) return 48;
            if (type == typeof(C49)) return 49;
            if (type == typeof(C50)) return 50;
            if (type == typeof(C51)) return 51;
            if (type == typeof(C52)) return 52;
            if (type == typeof(C53)) return 53;
            if (type == typeof(C54)) return 54;
            if (type == typeof(C55)) return 55;
            if (type == typeof(C56)) return 56;
            if (type == typeof(C57)) return 57;
            if (type == typeof(C58)) return 58;
            if (type == typeof(C59)) return 59;
            if (type == typeof(C60)) return 60;
            if (type == typeof(C61)) return 61;
            if (type == typeof(C62)) return 62;
            if (type == typeof(C63)) return 63;
            return -1;
        }

        public static int IfChainType512(Type type)
        {
            if (type == typeof(C0)) return 0;
            if (type == typeof(C1)) return 1;
            if (type == typeof(C2)) return 2;
            if (type == typeof(C3)) return 3;
            if (type == typeof(C4)) return 4;
            if (type == typeof(C5)) return 5;
            if (type == typeof(C6)) return 6;
            if (type == typeof(C7)) return 7;
            if (type == typeof(C8)) return 8;
            if (type == typeof(C9)) return 9;
            if (type == typeof(C10)) return 10;
            if (type == typeof(C11)) return 11;
            if (type == typeof(C12)) return 12;
            if (type == typeof(C13)) return 13;
            if (type == typeof(C14)) return 14;
            if (type == typeof(C15)) return 15;
            if (type == typeof(C16)) return 16;
            if (type == typeof(C17)) return 17;
            if (type == typeof(C18)) return 18;
            if (type == typeof(C19)) return 19;
            if (type == typeof(C20)) return 20;
            if (type == typeof(C21)) return 21;
            if (type == typeof(C22)) return 22;
            if (type == typeof(C23)) return 23;
            if (type == typeof(C24)) return 24;
            if (type == typeof(C25)) return 25;
            if (type == typeof(C26)) return 26;
            if (type == typeof(C27)) return 27;
            if (type == typeof(C28)) return 28;
            if (type == typeof(C29)) return 29;
            if (type == typeof(C30)) return 30;
            if (type == typeof(C31)) return 31;
            if (type == typeof(C32)) return 32;
            if (type == typeof(C33)) return 33;
            if (type == typeof(C34)) return 34;
            if (type == typeof(C35)) return 35;
            if (type == typeof(C36)) return 36;
            if (type == typeof(C37)) return 37;
            if (type == typeof(C38)) return 38;
            if (type == typeof(C39)) return 39;
            if (type == typeof(C40)) return 40;
            if (type == typeof(C41)) return 41;
            if (type == typeof(C42)) return 42;
            if (type == typeof(C43)) return 43;
            if (type == typeof(C44)) return 44;
            if (type == typeof(C45)) return 45;
            if (type == typeof(C46)) return 46;
            if (type == typeof(C47)) return 47;
            if (type == typeof(C48)) return 48;
            if (type == typeof(C49)) return 49;
            if (type == typeof(C50)) return 50;
            if (type == typeof(C51)) return 51;
            if (type == typeof(C52)) return 52;
            if (type == typeof(C53)) return 53;
            if (type == typeof(C54)) return 54;
            if (type == typeof(C55)) return 55;
            if (type == typeof(C56)) return 56;
            if (type == typeof(C57)) return 57;
            if (type == typeof(C58)) return 58;
            if (type == typeof(C59)) return 59;
            if (type == typeof(C60)) return 60;
            if (type == typeof(C61)) return 61;
            if (type == typeof(C62)) return 62;
            if (type == typeof(C63)) return 63;
            if (type == typeof(C64)) return 64;
            if (type == typeof(C65)) return 65;
            if (type == typeof(C66)) return 66;
            if (type == typeof(C67)) return 67;
            if (type == typeof(C68)) return 68;
            if (type == typeof(C69)) return 69;
            if (type == typeof(C70)) return 70;
            if (type == typeof(C71)) return 71;
            if (type == typeof(C72)) return 72;
            if (type == typeof(C73)) return 73;
            if (type == typeof(C74)) return 74;
            if (type == typeof(C75)) return 75;
            if (type == typeof(C76)) return 76;
            if (type == typeof(C77)) return 77;
            if (type == typeof(C78)) return 78;
            if (type == typeof(C79)) return 79;
            if (type == typeof(C80)) return 80;
            if (type == typeof(C81)) return 81;
            if (type == typeof(C82)) return 82;
            if (type == typeof(C83)) return 83;
            if (type == typeof(C84)) return 84;
            if (type == typeof(C85)) return 85;
            if (type == typeof(C86)) return 86;
            if (type == typeof(C87)) return 87;
            if (type == typeof(C88)) return 88;
            if (type == typeof(C89)) return 89;
            if (type == typeof(C90)) return 90;
            if (type == typeof(C91)) return 91;
            if (type == typeof(C92)) return 92;
            if (type == typeof(C93)) return 93;
            if (type == typeof(C94)) return 94;
            if (type == typeof(C95)) return 95;
            if (type == typeof(C96)) return 96;
            if (type == typeof(C97)) return 97;
            if (type == typeof(C98)) return 98;
            if (type == typeof(C99)) return 99;
            if (type == typeof(C100)) return 100;
            if (type == typeof(C101)) return 101;
            if (type == typeof(C102)) return 102;
            if (type == typeof(C103)) return 103;
            if (type == typeof(C104)) return 104;
            if (type == typeof(C105)) return 105;
            if (type == typeof(C106)) return 106;
            if (type == typeof(C107)) return 107;
            if (type == typeof(C108)) return 108;
            if (type == typeof(C109)) return 109;
            if (type == typeof(C110)) return 110;
            if (type == typeof(C111)) return 111;
            if (type == typeof(C112)) return 112;
            if (type == typeof(C113)) return 113;
            if (type == typeof(C114)) return 114;
            if (type == typeof(C115)) return 115;
            if (type == typeof(C116)) return 116;
            if (type == typeof(C117)) return 117;
            if (type == typeof(C118)) return 118;
            if (type == typeof(C119)) return 119;
            if (type == typeof(C120)) return 120;
            if (type == typeof(C121)) return 121;
            if (type == typeof(C122)) return 122;
            if (type == typeof(C123)) return 123;
            if (type == typeof(C124)) return 124;
            if (type == typeof(C125)) return 125;
            if (type == typeof(C126)) return 126;
            if (type == typeof(C127)) return 127;
            if (type == typeof(C128)) return 128;
            if (type == typeof(C129)) return 129;
            if (type == typeof(C130)) return 130;
            if (type == typeof(C131)) return 131;
            if (type == typeof(C132)) return 132;
            if (type == typeof(C133)) return 133;
            if (type == typeof(C134)) return 134;
            if (type == typeof(C135)) return 135;
            if (type == typeof(C136)) return 136;
            if (type == typeof(C137)) return 137;
            if (type == typeof(C138)) return 138;
            if (type == typeof(C139)) return 139;
            if (type == typeof(C140)) return 140;
            if (type == typeof(C141)) return 141;
            if (type == typeof(C142)) return 142;
            if (type == typeof(C143)) return 143;
            if (type == typeof(C144)) return 144;
            if (type == typeof(C145)) return 145;
            if (type == typeof(C146)) return 146;
            if (type == typeof(C147)) return 147;
            if (type == typeof(C148)) return 148;
            if (type == typeof(C149)) return 149;
            if (type == typeof(C150)) return 150;
            if (type == typeof(C151)) return 151;
            if (type == typeof(C152)) return 152;
            if (type == typeof(C153)) return 153;
            if (type == typeof(C154)) return 154;
            if (type == typeof(C155)) return 155;
            if (type == typeof(C156)) return 156;
            if (type == typeof(C157)) return 157;
            if (type == typeof(C158)) return 158;
            if (type == typeof(C159)) return 159;
            if (type == typeof(C160)) return 160;
            if (type == typeof(C161)) return 161;
            if (type == typeof(C162)) return 162;
            if (type == typeof(C163)) return 163;
            if (type == typeof(C164)) return 164;
            if (type == typeof(C165)) return 165;
            if (type == typeof(C166)) return 166;
            if (type == typeof(C167)) return 167;
            if (type == typeof(C168)) return 168;
            if (type == typeof(C169)) return 169;
            if (type == typeof(C170)) return 170;
            if (type == typeof(C171)) return 171;
            if (type == typeof(C172)) return 172;
            if (type == typeof(C173)) return 173;
            if (type == typeof(C174)) return 174;
            if (type == typeof(C175)) return 175;
            if (type == typeof(C176)) return 176;
            if (type == typeof(C177)) return 177;
            if (type == typeof(C178)) return 178;
            if (type == typeof(C179)) return 179;
            if (type == typeof(C180)) return 180;
            if (type == typeof(C181)) return 181;
            if (type == typeof(C182)) return 182;
            if (type == typeof(C183)) return 183;
            if (type == typeof(C184)) return 184;
            if (type == typeof(C185)) return 185;
            if (type == typeof(C186)) return 186;
            if (type == typeof(C187)) return 187;
            if (type == typeof(C188)) return 188;
            if (type == typeof(C189)) return 189;
            if (type == typeof(C190)) return 190;
            if (type == typeof(C191)) return 191;
            if (type == typeof(C192)) return 192;
            if (type == typeof(C193)) return 193;
            if (type == typeof(C194)) return 194;
            if (type == typeof(C195)) return 195;
            if (type == typeof(C196)) return 196;
            if (type == typeof(C197)) return 197;
            if (type == typeof(C198)) return 198;
            if (type == typeof(C199)) return 199;
            if (type == typeof(C200)) return 200;
            if (type == typeof(C201)) return 201;
            if (type == typeof(C202)) return 202;
            if (type == typeof(C203)) return 203;
            if (type == typeof(C204)) return 204;
            if (type == typeof(C205)) return 205;
            if (type == typeof(C206)) return 206;
            if (type == typeof(C207)) return 207;
            if (type == typeof(C208)) return 208;
            if (type == typeof(C209)) return 209;
            if (type == typeof(C210)) return 210;
            if (type == typeof(C211)) return 211;
            if (type == typeof(C212)) return 212;
            if (type == typeof(C213)) return 213;
            if (type == typeof(C214)) return 214;
            if (type == typeof(C215)) return 215;
            if (type == typeof(C216)) return 216;
            if (type == typeof(C217)) return 217;
            if (type == typeof(C218)) return 218;
            if (type == typeof(C219)) return 219;
            if (type == typeof(C220)) return 220;
            if (type == typeof(C221)) return 221;
            if (type == typeof(C222)) return 222;
            if (type == typeof(C223)) return 223;
            if (type == typeof(C224)) return 224;
            if (type == typeof(C225)) return 225;
            if (type == typeof(C226)) return 226;
            if (type == typeof(C227)) return 227;
            if (type == typeof(C228)) return 228;
            if (type == typeof(C229)) return 229;
            if (type == typeof(C230)) return 230;
            if (type == typeof(C231)) return 231;
            if (type == typeof(C232)) return 232;
            if (type == typeof(C233)) return 233;
            if (type == typeof(C234)) return 234;
            if (type == typeof(C235)) return 235;
            if (type == typeof(C236)) return 236;
            if (type == typeof(C237)) return 237;
            if (type == typeof(C238)) return 238;
            if (type == typeof(C239)) return 239;
            if (type == typeof(C240)) return 240;
            if (type == typeof(C241)) return 241;
            if (type == typeof(C242)) return 242;
            if (type == typeof(C243)) return 243;
            if (type == typeof(C244)) return 244;
            if (type == typeof(C245)) return 245;
            if (type == typeof(C246)) return 246;
            if (type == typeof(C247)) return 247;
            if (type == typeof(C248)) return 248;
            if (type == typeof(C249)) return 249;
            if (type == typeof(C250)) return 250;
            if (type == typeof(C251)) return 251;
            if (type == typeof(C252)) return 252;
            if (type == typeof(C253)) return 253;
            if (type == typeof(C254)) return 254;
            if (type == typeof(C255)) return 255;
            if (type == typeof(C256)) return 256;
            if (type == typeof(C257)) return 257;
            if (type == typeof(C258)) return 258;
            if (type == typeof(C259)) return 259;
            if (type == typeof(C260)) return 260;
            if (type == typeof(C261)) return 261;
            if (type == typeof(C262)) return 262;
            if (type == typeof(C263)) return 263;
            if (type == typeof(C264)) return 264;
            if (type == typeof(C265)) return 265;
            if (type == typeof(C266)) return 266;
            if (type == typeof(C267)) return 267;
            if (type == typeof(C268)) return 268;
            if (type == typeof(C269)) return 269;
            if (type == typeof(C270)) return 270;
            if (type == typeof(C271)) return 271;
            if (type == typeof(C272)) return 272;
            if (type == typeof(C273)) return 273;
            if (type == typeof(C274)) return 274;
            if (type == typeof(C275)) return 275;
            if (type == typeof(C276)) return 276;
            if (type == typeof(C277)) return 277;
            if (type == typeof(C278)) return 278;
            if (type == typeof(C279)) return 279;
            if (type == typeof(C280)) return 280;
            if (type == typeof(C281)) return 281;
            if (type == typeof(C282)) return 282;
            if (type == typeof(C283)) return 283;
            if (type == typeof(C284)) return 284;
            if (type == typeof(C285)) return 285;
            if (type == typeof(C286)) return 286;
            if (type == typeof(C287)) return 287;
            if (type == typeof(C288)) return 288;
            if (type == typeof(C289)) return 289;
            if (type == typeof(C290)) return 290;
            if (type == typeof(C291)) return 291;
            if (type == typeof(C292)) return 292;
            if (type == typeof(C293)) return 293;
            if (type == typeof(C294)) return 294;
            if (type == typeof(C295)) return 295;
            if (type == typeof(C296)) return 296;
            if (type == typeof(C297)) return 297;
            if (type == typeof(C298)) return 298;
            if (type == typeof(C299)) return 299;
            if (type == typeof(C300)) return 300;
            if (type == typeof(C301)) return 301;
            if (type == typeof(C302)) return 302;
            if (type == typeof(C303)) return 303;
            if (type == typeof(C304)) return 304;
            if (type == typeof(C305)) return 305;
            if (type == typeof(C306)) return 306;
            if (type == typeof(C307)) return 307;
            if (type == typeof(C308)) return 308;
            if (type == typeof(C309)) return 309;
            if (type == typeof(C310)) return 310;
            if (type == typeof(C311)) return 311;
            if (type == typeof(C312)) return 312;
            if (type == typeof(C313)) return 313;
            if (type == typeof(C314)) return 314;
            if (type == typeof(C315)) return 315;
            if (type == typeof(C316)) return 316;
            if (type == typeof(C317)) return 317;
            if (type == typeof(C318)) return 318;
            if (type == typeof(C319)) return 319;
            if (type == typeof(C320)) return 320;
            if (type == typeof(C321)) return 321;
            if (type == typeof(C322)) return 322;
            if (type == typeof(C323)) return 323;
            if (type == typeof(C324)) return 324;
            if (type == typeof(C325)) return 325;
            if (type == typeof(C326)) return 326;
            if (type == typeof(C327)) return 327;
            if (type == typeof(C328)) return 328;
            if (type == typeof(C329)) return 329;
            if (type == typeof(C330)) return 330;
            if (type == typeof(C331)) return 331;
            if (type == typeof(C332)) return 332;
            if (type == typeof(C333)) return 333;
            if (type == typeof(C334)) return 334;
            if (type == typeof(C335)) return 335;
            if (type == typeof(C336)) return 336;
            if (type == typeof(C337)) return 337;
            if (type == typeof(C338)) return 338;
            if (type == typeof(C339)) return 339;
            if (type == typeof(C340)) return 340;
            if (type == typeof(C341)) return 341;
            if (type == typeof(C342)) return 342;
            if (type == typeof(C343)) return 343;
            if (type == typeof(C344)) return 344;
            if (type == typeof(C345)) return 345;
            if (type == typeof(C346)) return 346;
            if (type == typeof(C347)) return 347;
            if (type == typeof(C348)) return 348;
            if (type == typeof(C349)) return 349;
            if (type == typeof(C350)) return 350;
            if (type == typeof(C351)) return 351;
            if (type == typeof(C352)) return 352;
            if (type == typeof(C353)) return 353;
            if (type == typeof(C354)) return 354;
            if (type == typeof(C355)) return 355;
            if (type == typeof(C356)) return 356;
            if (type == typeof(C357)) return 357;
            if (type == typeof(C358)) return 358;
            if (type == typeof(C359)) return 359;
            if (type == typeof(C360)) return 360;
            if (type == typeof(C361)) return 361;
            if (type == typeof(C362)) return 362;
            if (type == typeof(C363)) return 363;
            if (type == typeof(C364)) return 364;
            if (type == typeof(C365)) return 365;
            if (type == typeof(C366)) return 366;
            if (type == typeof(C367)) return 367;
            if (type == typeof(C368)) return 368;
            if (type == typeof(C369)) return 369;
            if (type == typeof(C370)) return 370;
            if (type == typeof(C371)) return 371;
            if (type == typeof(C372)) return 372;
            if (type == typeof(C373)) return 373;
            if (type == typeof(C374)) return 374;
            if (type == typeof(C375)) return 375;
            if (type == typeof(C376)) return 376;
            if (type == typeof(C377)) return 377;
            if (type == typeof(C378)) return 378;
            if (type == typeof(C379)) return 379;
            if (type == typeof(C380)) return 380;
            if (type == typeof(C381)) return 381;
            if (type == typeof(C382)) return 382;
            if (type == typeof(C383)) return 383;
            if (type == typeof(C384)) return 384;
            if (type == typeof(C385)) return 385;
            if (type == typeof(C386)) return 386;
            if (type == typeof(C387)) return 387;
            if (type == typeof(C388)) return 388;
            if (type == typeof(C389)) return 389;
            if (type == typeof(C390)) return 390;
            if (type == typeof(C391)) return 391;
            if (type == typeof(C392)) return 392;
            if (type == typeof(C393)) return 393;
            if (type == typeof(C394)) return 394;
            if (type == typeof(C395)) return 395;
            if (type == typeof(C396)) return 396;
            if (type == typeof(C397)) return 397;
            if (type == typeof(C398)) return 398;
            if (type == typeof(C399)) return 399;
            if (type == typeof(C400)) return 400;
            if (type == typeof(C401)) return 401;
            if (type == typeof(C402)) return 402;
            if (type == typeof(C403)) return 403;
            if (type == typeof(C404)) return 404;
            if (type == typeof(C405)) return 405;
            if (type == typeof(C406)) return 406;
            if (type == typeof(C407)) return 407;
            if (type == typeof(C408)) return 408;
            if (type == typeof(C409)) return 409;
            if (type == typeof(C410)) return 410;
            if (type == typeof(C411)) return 411;
            if (type == typeof(C412)) return 412;
            if (type == typeof(C413)) return 413;
            if (type == typeof(C414)) return 414;
            if (type == typeof(C415)) return 415;
            if (type == typeof(C416)) return 416;
            if (type == typeof(C417)) return 417;
            if (type == typeof(C418)) return 418;
            if (type == typeof(C419)) return 419;
            if (type == typeof(C420)) return 420;
            if (type == typeof(C421)) return 421;
            if (type == typeof(C422)) return 422;
            if (type == typeof(C423)) return 423;
            if (type == typeof(C424)) return 424;
            if (type == typeof(C425)) return 425;
            if (type == typeof(C426)) return 426;
            if (type == typeof(C427)) return 427;
            if (type == typeof(C428)) return 428;
            if (type == typeof(C429)) return 429;
            if (type == typeof(C430)) return 430;
            if (type == typeof(C431)) return 431;
            if (type == typeof(C432)) return 432;
            if (type == typeof(C433)) return 433;
            if (type == typeof(C434)) return 434;
            if (type == typeof(C435)) return 435;
            if (type == typeof(C436)) return 436;
            if (type == typeof(C437)) return 437;
            if (type == typeof(C438)) return 438;
            if (type == typeof(C439)) return 439;
            if (type == typeof(C440)) return 440;
            if (type == typeof(C441)) return 441;
            if (type == typeof(C442)) return 442;
            if (type == typeof(C443)) return 443;
            if (type == typeof(C444)) return 444;
            if (type == typeof(C445)) return 445;
            if (type == typeof(C446)) return 446;
            if (type == typeof(C447)) return 447;
            if (type == typeof(C448)) return 448;
            if (type == typeof(C449)) return 449;
            if (type == typeof(C450)) return 450;
            if (type == typeof(C451)) return 451;
            if (type == typeof(C452)) return 452;
            if (type == typeof(C453)) return 453;
            if (type == typeof(C454)) return 454;
            if (type == typeof(C455)) return 455;
            if (type == typeof(C456)) return 456;
            if (type == typeof(C457)) return 457;
            if (type == typeof(C458)) return 458;
            if (type == typeof(C459)) return 459;
            if (type == typeof(C460)) return 460;
            if (type == typeof(C461)) return 461;
            if (type == typeof(C462)) return 462;
            if (type == typeof(C463)) return 463;
            if (type == typeof(C464)) return 464;
            if (type == typeof(C465)) return 465;
            if (type == typeof(C466)) return 466;
            if (type == typeof(C467)) return 467;
            if (type == typeof(C468)) return 468;
            if (type == typeof(C469)) return 469;
            if (type == typeof(C470)) return 470;
            if (type == typeof(C471)) return 471;
            if (type == typeof(C472)) return 472;
            if (type == typeof(C473)) return 473;
            if (type == typeof(C474)) return 474;
            if (type == typeof(C475)) return 475;
            if (type == typeof(C476)) return 476;
            if (type == typeof(C477)) return 477;
            if (type == typeof(C478)) return 478;
            if (type == typeof(C479)) return 479;
            if (type == typeof(C480)) return 480;
            if (type == typeof(C481)) return 481;
            if (type == typeof(C482)) return 482;
            if (type == typeof(C483)) return 483;
            if (type == typeof(C484)) return 484;
            if (type == typeof(C485)) return 485;
            if (type == typeof(C486)) return 486;
            if (type == typeof(C487)) return 487;
            if (type == typeof(C488)) return 488;
            if (type == typeof(C489)) return 489;
            if (type == typeof(C490)) return 490;
            if (type == typeof(C491)) return 491;
            if (type == typeof(C492)) return 492;
            if (type == typeof(C493)) return 493;
            if (type == typeof(C494)) return 494;
            if (type == typeof(C495)) return 495;
            if (type == typeof(C496)) return 496;
            if (type == typeof(C497)) return 497;
            if (type == typeof(C498)) return 498;
            if (type == typeof(C499)) return 499;
            if (type == typeof(C500)) return 500;
            if (type == typeof(C501)) return 501;
            if (type == typeof(C502)) return 502;
            if (type == typeof(C503)) return 503;
            if (type == typeof(C504)) return 504;
            if (type == typeof(C505)) return 505;
            if (type == typeof(C506)) return 506;
            if (type == typeof(C507)) return 507;
            if (type == typeof(C508)) return 508;
            if (type == typeof(C509)) return 509;
            if (type == typeof(C510)) return 510;
            if (type == typeof(C511)) return 511;
            return -1;
        }

        public static int IfChainGeneric8<T>()
        {
            if (typeof(T) == typeof(C0)) return 0;
            if (typeof(T) == typeof(C1)) return 1;
            if (typeof(T) == typeof(C2)) return 2;
            if (typeof(T) == typeof(C3)) return 3;
            if (typeof(T) == typeof(C4)) return 4;
            if (typeof(T) == typeof(C5)) return 5;
            if (typeof(T) == typeof(C6)) return 6;
            if (typeof(T) == typeof(C7)) return 7;
            return -1;
        }

        public static int IfChainGeneric64<T>()
        {
            if (typeof(T) == typeof(C0)) return 0;
            if (typeof(T) == typeof(C1)) return 1;
            if (typeof(T) == typeof(C2)) return 2;
            if (typeof(T) == typeof(C3)) return 3;
            if (typeof(T) == typeof(C4)) return 4;
            if (typeof(T) == typeof(C5)) return 5;
            if (typeof(T) == typeof(C6)) return 6;
            if (typeof(T) == typeof(C7)) return 7;
            if (typeof(T) == typeof(C8)) return 8;
            if (typeof(T) == typeof(C9)) return 9;
            if (typeof(T) == typeof(C10)) return 10;
            if (typeof(T) == typeof(C11)) return 11;
            if (typeof(T) == typeof(C12)) return 12;
            if (typeof(T) == typeof(C13)) return 13;
            if (typeof(T) == typeof(C14)) return 14;
            if (typeof(T) == typeof(C15)) return 15;
            if (typeof(T) == typeof(C16)) return 16;
            if (typeof(T) == typeof(C17)) return 17;
            if (typeof(T) == typeof(C18)) return 18;
            if (typeof(T) == typeof(C19)) return 19;
            if (typeof(T) == typeof(C20)) return 20;
            if (typeof(T) == typeof(C21)) return 21;
            if (typeof(T) == typeof(C22)) return 22;
            if (typeof(T) == typeof(C23)) return 23;
            if (typeof(T) == typeof(C24)) return 24;
            if (typeof(T) == typeof(C25)) return 25;
            if (typeof(T) == typeof(C26)) return 26;
            if (typeof(T) == typeof(C27)) return 27;
            if (typeof(T) == typeof(C28)) return 28;
            if (typeof(T) == typeof(C29)) return 29;
            if (typeof(T) == typeof(C30)) return 30;
            if (typeof(T) == typeof(C31)) return 31;
            if (typeof(T) == typeof(C32)) return 32;
            if (typeof(T) == typeof(C33)) return 33;
            if (typeof(T) == typeof(C34)) return 34;
            if (typeof(T) == typeof(C35)) return 35;
            if (typeof(T) == typeof(C36)) return 36;
            if (typeof(T) == typeof(C37)) return 37;
            if (typeof(T) == typeof(C38)) return 38;
            if (typeof(T) == typeof(C39)) return 39;
            if (typeof(T) == typeof(C40)) return 40;
            if (typeof(T) == typeof(C41)) return 41;
            if (typeof(T) == typeof(C42)) return 42;
            if (typeof(T) == typeof(C43)) return 43;
            if (typeof(T) == typeof(C44)) return 44;
            if (typeof(T) == typeof(C45)) return 45;
            if (typeof(T) == typeof(C46)) return 46;
            if (typeof(T) == typeof(C47)) return 47;
            if (typeof(T) == typeof(C48)) return 48;
            if (typeof(T) == typeof(C49)) return 49;
            if (typeof(T) == typeof(C50)) return 50;
            if (typeof(T) == typeof(C51)) return 51;
            if (typeof(T) == typeof(C52)) return 52;
            if (typeof(T) == typeof(C53)) return 53;
            if (typeof(T) == typeof(C54)) return 54;
            if (typeof(T) == typeof(C55)) return 55;
            if (typeof(T) == typeof(C56)) return 56;
            if (typeof(T) == typeof(C57)) return 57;
            if (typeof(T) == typeof(C58)) return 58;
            if (typeof(T) == typeof(C59)) return 59;
            if (typeof(T) == typeof(C60)) return 60;
            if (typeof(T) == typeof(C61)) return 61;
            if (typeof(T) == typeof(C62)) return 62;
            if (typeof(T) == typeof(C63)) return 63;
            return -1;
        }

        public static int IfChainGeneric512<T>()
        {
            if (typeof(T) == typeof(C0)) return 0;
            if (typeof(T) == typeof(C1)) return 1;
            if (typeof(T) == typeof(C2)) return 2;
            if (typeof(T) == typeof(C3)) return 3;
            if (typeof(T) == typeof(C4)) return 4;
            if (typeof(T) == typeof(C5)) return 5;
            if (typeof(T) == typeof(C6)) return 6;
            if (typeof(T) == typeof(C7)) return 7;
            if (typeof(T) == typeof(C8)) return 8;
            if (typeof(T) == typeof(C9)) return 9;
            if (typeof(T) == typeof(C10)) return 10;
            if (typeof(T) == typeof(C11)) return 11;
            if (typeof(T) == typeof(C12)) return 12;
            if (typeof(T) == typeof(C13)) return 13;
            if (typeof(T) == typeof(C14)) return 14;
            if (typeof(T) == typeof(C15)) return 15;
            if (typeof(T) == typeof(C16)) return 16;
            if (typeof(T) == typeof(C17)) return 17;
            if (typeof(T) == typeof(C18)) return 18;
            if (typeof(T) == typeof(C19)) return 19;
            if (typeof(T) == typeof(C20)) return 20;
            if (typeof(T) == typeof(C21)) return 21;
            if (typeof(T) == typeof(C22)) return 22;
            if (typeof(T) == typeof(C23)) return 23;
            if (typeof(T) == typeof(C24)) return 24;
            if (typeof(T) == typeof(C25)) return 25;
            if (typeof(T) == typeof(C26)) return 26;
            if (typeof(T) == typeof(C27)) return 27;
            if (typeof(T) == typeof(C28)) return 28;
            if (typeof(T) == typeof(C29)) return 29;
            if (typeof(T) == typeof(C30)) return 30;
            if (typeof(T) == typeof(C31)) return 31;
            if (typeof(T) == typeof(C32)) return 32;
            if (typeof(T) == typeof(C33)) return 33;
            if (typeof(T) == typeof(C34)) return 34;
            if (typeof(T) == typeof(C35)) return 35;
            if (typeof(T) == typeof(C36)) return 36;
            if (typeof(T) == typeof(C37)) return 37;
            if (typeof(T) == typeof(C38)) return 38;
            if (typeof(T) == typeof(C39)) return 39;
            if (typeof(T) == typeof(C40)) return 40;
            if (typeof(T) == typeof(C41)) return 41;
            if (typeof(T) == typeof(C42)) return 42;
            if (typeof(T) == typeof(C43)) return 43;
            if (typeof(T) == typeof(C44)) return 44;
            if (typeof(T) == typeof(C45)) return 45;
            if (typeof(T) == typeof(C46)) return 46;
            if (typeof(T) == typeof(C47)) return 47;
            if (typeof(T) == typeof(C48)) return 48;
            if (typeof(T) == typeof(C49)) return 49;
            if (typeof(T) == typeof(C50)) return 50;
            if (typeof(T) == typeof(C51)) return 51;
            if (typeof(T) == typeof(C52)) return 52;
            if (typeof(T) == typeof(C53)) return 53;
            if (typeof(T) == typeof(C54)) return 54;
            if (typeof(T) == typeof(C55)) return 55;
            if (typeof(T) == typeof(C56)) return 56;
            if (typeof(T) == typeof(C57)) return 57;
            if (typeof(T) == typeof(C58)) return 58;
            if (typeof(T) == typeof(C59)) return 59;
            if (typeof(T) == typeof(C60)) return 60;
            if (typeof(T) == typeof(C61)) return 61;
            if (typeof(T) == typeof(C62)) return 62;
            if (typeof(T) == typeof(C63)) return 63;
            if (typeof(T) == typeof(C64)) return 64;
            if (typeof(T) == typeof(C65)) return 65;
            if (typeof(T) == typeof(C66)) return 66;
            if (typeof(T) == typeof(C67)) return 67;
            if (typeof(T) == typeof(C68)) return 68;
            if (typeof(T) == typeof(C69)) return 69;
            if (typeof(T) == typeof(C70)) return 70;
            if (typeof(T) == typeof(C71)) return 71;
            if (typeof(T) == typeof(C72)) return 72;
            if (typeof(T) == typeof(C73)) return 73;
            if (typeof(T) == typeof(C74)) return 74;
            if (typeof(T) == typeof(C75)) return 75;
            if (typeof(T) == typeof(C76)) return 76;
            if (typeof(T) == typeof(C77)) return 77;
            if (typeof(T) == typeof(C78)) return 78;
            if (typeof(T) == typeof(C79)) return 79;
            if (typeof(T) == typeof(C80)) return 80;
            if (typeof(T) == typeof(C81)) return 81;
            if (typeof(T) == typeof(C82)) return 82;
            if (typeof(T) == typeof(C83)) return 83;
            if (typeof(T) == typeof(C84)) return 84;
            if (typeof(T) == typeof(C85)) return 85;
            if (typeof(T) == typeof(C86)) return 86;
            if (typeof(T) == typeof(C87)) return 87;
            if (typeof(T) == typeof(C88)) return 88;
            if (typeof(T) == typeof(C89)) return 89;
            if (typeof(T) == typeof(C90)) return 90;
            if (typeof(T) == typeof(C91)) return 91;
            if (typeof(T) == typeof(C92)) return 92;
            if (typeof(T) == typeof(C93)) return 93;
            if (typeof(T) == typeof(C94)) return 94;
            if (typeof(T) == typeof(C95)) return 95;
            if (typeof(T) == typeof(C96)) return 96;
            if (typeof(T) == typeof(C97)) return 97;
            if (typeof(T) == typeof(C98)) return 98;
            if (typeof(T) == typeof(C99)) return 99;
            if (typeof(T) == typeof(C100)) return 100;
            if (typeof(T) == typeof(C101)) return 101;
            if (typeof(T) == typeof(C102)) return 102;
            if (typeof(T) == typeof(C103)) return 103;
            if (typeof(T) == typeof(C104)) return 104;
            if (typeof(T) == typeof(C105)) return 105;
            if (typeof(T) == typeof(C106)) return 106;
            if (typeof(T) == typeof(C107)) return 107;
            if (typeof(T) == typeof(C108)) return 108;
            if (typeof(T) == typeof(C109)) return 109;
            if (typeof(T) == typeof(C110)) return 110;
            if (typeof(T) == typeof(C111)) return 111;
            if (typeof(T) == typeof(C112)) return 112;
            if (typeof(T) == typeof(C113)) return 113;
            if (typeof(T) == typeof(C114)) return 114;
            if (typeof(T) == typeof(C115)) return 115;
            if (typeof(T) == typeof(C116)) return 116;
            if (typeof(T) == typeof(C117)) return 117;
            if (typeof(T) == typeof(C118)) return 118;
            if (typeof(T) == typeof(C119)) return 119;
            if (typeof(T) == typeof(C120)) return 120;
            if (typeof(T) == typeof(C121)) return 121;
            if (typeof(T) == typeof(C122)) return 122;
            if (typeof(T) == typeof(C123)) return 123;
            if (typeof(T) == typeof(C124)) return 124;
            if (typeof(T) == typeof(C125)) return 125;
            if (typeof(T) == typeof(C126)) return 126;
            if (typeof(T) == typeof(C127)) return 127;
            if (typeof(T) == typeof(C128)) return 128;
            if (typeof(T) == typeof(C129)) return 129;
            if (typeof(T) == typeof(C130)) return 130;
            if (typeof(T) == typeof(C131)) return 131;
            if (typeof(T) == typeof(C132)) return 132;
            if (typeof(T) == typeof(C133)) return 133;
            if (typeof(T) == typeof(C134)) return 134;
            if (typeof(T) == typeof(C135)) return 135;
            if (typeof(T) == typeof(C136)) return 136;
            if (typeof(T) == typeof(C137)) return 137;
            if (typeof(T) == typeof(C138)) return 138;
            if (typeof(T) == typeof(C139)) return 139;
            if (typeof(T) == typeof(C140)) return 140;
            if (typeof(T) == typeof(C141)) return 141;
            if (typeof(T) == typeof(C142)) return 142;
            if (typeof(T) == typeof(C143)) return 143;
            if (typeof(T) == typeof(C144)) return 144;
            if (typeof(T) == typeof(C145)) return 145;
            if (typeof(T) == typeof(C146)) return 146;
            if (typeof(T) == typeof(C147)) return 147;
            if (typeof(T) == typeof(C148)) return 148;
            if (typeof(T) == typeof(C149)) return 149;
            if (typeof(T) == typeof(C150)) return 150;
            if (typeof(T) == typeof(C151)) return 151;
            if (typeof(T) == typeof(C152)) return 152;
            if (typeof(T) == typeof(C153)) return 153;
            if (typeof(T) == typeof(C154)) return 154;
            if (typeof(T) == typeof(C155)) return 155;
            if (typeof(T) == typeof(C156)) return 156;
            if (typeof(T) == typeof(C157)) return 157;
            if (typeof(T) == typeof(C158)) return 158;
            if (typeof(T) == typeof(C159)) return 159;
            if (typeof(T) == typeof(C160)) return 160;
            if (typeof(T) == typeof(C161)) return 161;
            if (typeof(T) == typeof(C162)) return 162;
            if (typeof(T) == typeof(C163)) return 163;
            if (typeof(T) == typeof(C164)) return 164;
            if (typeof(T) == typeof(C165)) return 165;
            if (typeof(T) == typeof(C166)) return 166;
            if (typeof(T) == typeof(C167)) return 167;
            if (typeof(T) == typeof(C168)) return 168;
            if (typeof(T) == typeof(C169)) return 169;
            if (typeof(T) == typeof(C170)) return 170;
            if (typeof(T) == typeof(C171)) return 171;
            if (typeof(T) == typeof(C172)) return 172;
            if (typeof(T) == typeof(C173)) return 173;
            if (typeof(T) == typeof(C174)) return 174;
            if (typeof(T) == typeof(C175)) return 175;
            if (typeof(T) == typeof(C176)) return 176;
            if (typeof(T) == typeof(C177)) return 177;
            if (typeof(T) == typeof(C178)) return 178;
            if (typeof(T) == typeof(C179)) return 179;
            if (typeof(T) == typeof(C180)) return 180;
            if (typeof(T) == typeof(C181)) return 181;
            if (typeof(T) == typeof(C182)) return 182;
            if (typeof(T) == typeof(C183)) return 183;
            if (typeof(T) == typeof(C184)) return 184;
            if (typeof(T) == typeof(C185)) return 185;
            if (typeof(T) == typeof(C186)) return 186;
            if (typeof(T) == typeof(C187)) return 187;
            if (typeof(T) == typeof(C188)) return 188;
            if (typeof(T) == typeof(C189)) return 189;
            if (typeof(T) == typeof(C190)) return 190;
            if (typeof(T) == typeof(C191)) return 191;
            if (typeof(T) == typeof(C192)) return 192;
            if (typeof(T) == typeof(C193)) return 193;
            if (typeof(T) == typeof(C194)) return 194;
            if (typeof(T) == typeof(C195)) return 195;
            if (typeof(T) == typeof(C196)) return 196;
            if (typeof(T) == typeof(C197)) return 197;
            if (typeof(T) == typeof(C198)) return 198;
            if (typeof(T) == typeof(C199)) return 199;
            if (typeof(T) == typeof(C200)) return 200;
            if (typeof(T) == typeof(C201)) return 201;
            if (typeof(T) == typeof(C202)) return 202;
            if (typeof(T) == typeof(C203)) return 203;
            if (typeof(T) == typeof(C204)) return 204;
            if (typeof(T) == typeof(C205)) return 205;
            if (typeof(T) == typeof(C206)) return 206;
            if (typeof(T) == typeof(C207)) return 207;
            if (typeof(T) == typeof(C208)) return 208;
            if (typeof(T) == typeof(C209)) return 209;
            if (typeof(T) == typeof(C210)) return 210;
            if (typeof(T) == typeof(C211)) return 211;
            if (typeof(T) == typeof(C212)) return 212;
            if (typeof(T) == typeof(C213)) return 213;
            if (typeof(T) == typeof(C214)) return 214;
            if (typeof(T) == typeof(C215)) return 215;
            if (typeof(T) == typeof(C216)) return 216;
            if (typeof(T) == typeof(C217)) return 217;
            if (typeof(T) == typeof(C218)) return 218;
            if (typeof(T) == typeof(C219)) return 219;
            if (typeof(T) == typeof(C220)) return 220;
            if (typeof(T) == typeof(C221)) return 221;
            if (typeof(T) == typeof(C222)) return 222;
            if (typeof(T) == typeof(C223)) return 223;
            if (typeof(T) == typeof(C224)) return 224;
            if (typeof(T) == typeof(C225)) return 225;
            if (typeof(T) == typeof(C226)) return 226;
            if (typeof(T) == typeof(C227)) return 227;
            if (typeof(T) == typeof(C228)) return 228;
            if (typeof(T) == typeof(C229)) return 229;
            if (typeof(T) == typeof(C230)) return 230;
            if (typeof(T) == typeof(C231)) return 231;
            if (typeof(T) == typeof(C232)) return 232;
            if (typeof(T) == typeof(C233)) return 233;
            if (typeof(T) == typeof(C234)) return 234;
            if (typeof(T) == typeof(C235)) return 235;
            if (typeof(T) == typeof(C236)) return 236;
            if (typeof(T) == typeof(C237)) return 237;
            if (typeof(T) == typeof(C238)) return 238;
            if (typeof(T) == typeof(C239)) return 239;
            if (typeof(T) == typeof(C240)) return 240;
            if (typeof(T) == typeof(C241)) return 241;
            if (typeof(T) == typeof(C242)) return 242;
            if (typeof(T) == typeof(C243)) return 243;
            if (typeof(T) == typeof(C244)) return 244;
            if (typeof(T) == typeof(C245)) return 245;
            if (typeof(T) == typeof(C246)) return 246;
            if (typeof(T) == typeof(C247)) return 247;
            if (typeof(T) == typeof(C248)) return 248;
            if (typeof(T) == typeof(C249)) return 249;
            if (typeof(T) == typeof(C250)) return 250;
            if (typeof(T) == typeof(C251)) return 251;
            if (typeof(T) == typeof(C252)) return 252;
            if (typeof(T) == typeof(C253)) return 253;
            if (typeof(T) == typeof(C254)) return 254;
            if (typeof(T) == typeof(C255)) return 255;
            if (typeof(T) == typeof(C256)) return 256;
            if (typeof(T) == typeof(C257)) return 257;
            if (typeof(T) == typeof(C258)) return 258;
            if (typeof(T) == typeof(C259)) return 259;
            if (typeof(T) == typeof(C260)) return 260;
            if (typeof(T) == typeof(C261)) return 261;
            if (typeof(T) == typeof(C262)) return 262;
            if (typeof(T) == typeof(C263)) return 263;
            if (typeof(T) == typeof(C264)) return 264;
            if (typeof(T) == typeof(C265)) return 265;
            if (typeof(T) == typeof(C266)) return 266;
            if (typeof(T) == typeof(C267)) return 267;
            if (typeof(T) == typeof(C268)) return 268;
            if (typeof(T) == typeof(C269)) return 269;
            if (typeof(T) == typeof(C270)) return 270;
            if (typeof(T) == typeof(C271)) return 271;
            if (typeof(T) == typeof(C272)) return 272;
            if (typeof(T) == typeof(C273)) return 273;
            if (typeof(T) == typeof(C274)) return 274;
            if (typeof(T) == typeof(C275)) return 275;
            if (typeof(T) == typeof(C276)) return 276;
            if (typeof(T) == typeof(C277)) return 277;
            if (typeof(T) == typeof(C278)) return 278;
            if (typeof(T) == typeof(C279)) return 279;
            if (typeof(T) == typeof(C280)) return 280;
            if (typeof(T) == typeof(C281)) return 281;
            if (typeof(T) == typeof(C282)) return 282;
            if (typeof(T) == typeof(C283)) return 283;
            if (typeof(T) == typeof(C284)) return 284;
            if (typeof(T) == typeof(C285)) return 285;
            if (typeof(T) == typeof(C286)) return 286;
            if (typeof(T) == typeof(C287)) return 287;
            if (typeof(T) == typeof(C288)) return 288;
            if (typeof(T) == typeof(C289)) return 289;
            if (typeof(T) == typeof(C290)) return 290;
            if (typeof(T) == typeof(C291)) return 291;
            if (typeof(T) == typeof(C292)) return 292;
            if (typeof(T) == typeof(C293)) return 293;
            if (typeof(T) == typeof(C294)) return 294;
            if (typeof(T) == typeof(C295)) return 295;
            if (typeof(T) == typeof(C296)) return 296;
            if (typeof(T) == typeof(C297)) return 297;
            if (typeof(T) == typeof(C298)) return 298;
            if (typeof(T) == typeof(C299)) return 299;
            if (typeof(T) == typeof(C300)) return 300;
            if (typeof(T) == typeof(C301)) return 301;
            if (typeof(T) == typeof(C302)) return 302;
            if (typeof(T) == typeof(C303)) return 303;
            if (typeof(T) == typeof(C304)) return 304;
            if (typeof(T) == typeof(C305)) return 305;
            if (typeof(T) == typeof(C306)) return 306;
            if (typeof(T) == typeof(C307)) return 307;
            if (typeof(T) == typeof(C308)) return 308;
            if (typeof(T) == typeof(C309)) return 309;
            if (typeof(T) == typeof(C310)) return 310;
            if (typeof(T) == typeof(C311)) return 311;
            if (typeof(T) == typeof(C312)) return 312;
            if (typeof(T) == typeof(C313)) return 313;
            if (typeof(T) == typeof(C314)) return 314;
            if (typeof(T) == typeof(C315)) return 315;
            if (typeof(T) == typeof(C316)) return 316;
            if (typeof(T) == typeof(C317)) return 317;
            if (typeof(T) == typeof(C318)) return 318;
            if (typeof(T) == typeof(C319)) return 319;
            if (typeof(T) == typeof(C320)) return 320;
            if (typeof(T) == typeof(C321)) return 321;
            if (typeof(T) == typeof(C322)) return 322;
            if (typeof(T) == typeof(C323)) return 323;
            if (typeof(T) == typeof(C324)) return 324;
            if (typeof(T) == typeof(C325)) return 325;
            if (typeof(T) == typeof(C326)) return 326;
            if (typeof(T) == typeof(C327)) return 327;
            if (typeof(T) == typeof(C328)) return 328;
            if (typeof(T) == typeof(C329)) return 329;
            if (typeof(T) == typeof(C330)) return 330;
            if (typeof(T) == typeof(C331)) return 331;
            if (typeof(T) == typeof(C332)) return 332;
            if (typeof(T) == typeof(C333)) return 333;
            if (typeof(T) == typeof(C334)) return 334;
            if (typeof(T) == typeof(C335)) return 335;
            if (typeof(T) == typeof(C336)) return 336;
            if (typeof(T) == typeof(C337)) return 337;
            if (typeof(T) == typeof(C338)) return 338;
            if (typeof(T) == typeof(C339)) return 339;
            if (typeof(T) == typeof(C340)) return 340;
            if (typeof(T) == typeof(C341)) return 341;
            if (typeof(T) == typeof(C342)) return 342;
            if (typeof(T) == typeof(C343)) return 343;
            if (typeof(T) == typeof(C344)) return 344;
            if (typeof(T) == typeof(C345)) return 345;
            if (typeof(T) == typeof(C346)) return 346;
            if (typeof(T) == typeof(C347)) return 347;
            if (typeof(T) == typeof(C348)) return 348;
            if (typeof(T) == typeof(C349)) return 349;
            if (typeof(T) == typeof(C350)) return 350;
            if (typeof(T) == typeof(C351)) return 351;
            if (typeof(T) == typeof(C352)) return 352;
            if (typeof(T) == typeof(C353)) return 353;
            if (typeof(T) == typeof(C354)) return 354;
            if (typeof(T) == typeof(C355)) return 355;
            if (typeof(T) == typeof(C356)) return 356;
            if (typeof(T) == typeof(C357)) return 357;
            if (typeof(T) == typeof(C358)) return 358;
            if (typeof(T) == typeof(C359)) return 359;
            if (typeof(T) == typeof(C360)) return 360;
            if (typeof(T) == typeof(C361)) return 361;
            if (typeof(T) == typeof(C362)) return 362;
            if (typeof(T) == typeof(C363)) return 363;
            if (typeof(T) == typeof(C364)) return 364;
            if (typeof(T) == typeof(C365)) return 365;
            if (typeof(T) == typeof(C366)) return 366;
            if (typeof(T) == typeof(C367)) return 367;
            if (typeof(T) == typeof(C368)) return 368;
            if (typeof(T) == typeof(C369)) return 369;
            if (typeof(T) == typeof(C370)) return 370;
            if (typeof(T) == typeof(C371)) return 371;
            if (typeof(T) == typeof(C372)) return 372;
            if (typeof(T) == typeof(C373)) return 373;
            if (typeof(T) == typeof(C374)) return 374;
            if (typeof(T) == typeof(C375)) return 375;
            if (typeof(T) == typeof(C376)) return 376;
            if (typeof(T) == typeof(C377)) return 377;
            if (typeof(T) == typeof(C378)) return 378;
            if (typeof(T) == typeof(C379)) return 379;
            if (typeof(T) == typeof(C380)) return 380;
            if (typeof(T) == typeof(C381)) return 381;
            if (typeof(T) == typeof(C382)) return 382;
            if (typeof(T) == typeof(C383)) return 383;
            if (typeof(T) == typeof(C384)) return 384;
            if (typeof(T) == typeof(C385)) return 385;
            if (typeof(T) == typeof(C386)) return 386;
            if (typeof(T) == typeof(C387)) return 387;
            if (typeof(T) == typeof(C388)) return 388;
            if (typeof(T) == typeof(C389)) return 389;
            if (typeof(T) == typeof(C390)) return 390;
            if (typeof(T) == typeof(C391)) return 391;
            if (typeof(T) == typeof(C392)) return 392;
            if (typeof(T) == typeof(C393)) return 393;
            if (typeof(T) == typeof(C394)) return 394;
            if (typeof(T) == typeof(C395)) return 395;
            if (typeof(T) == typeof(C396)) return 396;
            if (typeof(T) == typeof(C397)) return 397;
            if (typeof(T) == typeof(C398)) return 398;
            if (typeof(T) == typeof(C399)) return 399;
            if (typeof(T) == typeof(C400)) return 400;
            if (typeof(T) == typeof(C401)) return 401;
            if (typeof(T) == typeof(C402)) return 402;
            if (typeof(T) == typeof(C403)) return 403;
            if (typeof(T) == typeof(C404)) return 404;
            if (typeof(T) == typeof(C405)) return 405;
            if (typeof(T) == typeof(C406)) return 406;
            if (typeof(T) == typeof(C407)) return 407;
            if (typeof(T) == typeof(C408)) return 408;
            if (typeof(T) == typeof(C409)) return 409;
            if (typeof(T) == typeof(C410)) return 410;
            if (typeof(T) == typeof(C411)) return 411;
            if (typeof(T) == typeof(C412)) return 412;
            if (typeof(T) == typeof(C413)) return 413;
            if (typeof(T) == typeof(C414)) return 414;
            if (typeof(T) == typeof(C415)) return 415;
            if (typeof(T) == typeof(C416)) return 416;
            if (typeof(T) == typeof(C417)) return 417;
            if (typeof(T) == typeof(C418)) return 418;
            if (typeof(T) == typeof(C419)) return 419;
            if (typeof(T) == typeof(C420)) return 420;
            if (typeof(T) == typeof(C421)) return 421;
            if (typeof(T) == typeof(C422)) return 422;
            if (typeof(T) == typeof(C423)) return 423;
            if (typeof(T) == typeof(C424)) return 424;
            if (typeof(T) == typeof(C425)) return 425;
            if (typeof(T) == typeof(C426)) return 426;
            if (typeof(T) == typeof(C427)) return 427;
            if (typeof(T) == typeof(C428)) return 428;
            if (typeof(T) == typeof(C429)) return 429;
            if (typeof(T) == typeof(C430)) return 430;
            if (typeof(T) == typeof(C431)) return 431;
            if (typeof(T) == typeof(C432)) return 432;
            if (typeof(T) == typeof(C433)) return 433;
            if (typeof(T) == typeof(C434)) return 434;
            if (typeof(T) == typeof(C435)) return 435;
            if (typeof(T) == typeof(C436)) return 436;
            if (typeof(T) == typeof(C437)) return 437;
            if (typeof(T) == typeof(C438)) return 438;
            if (typeof(T) == typeof(C439)) return 439;
            if (typeof(T) == typeof(C440)) return 440;
            if (typeof(T) == typeof(C441)) return 441;
            if (typeof(T) == typeof(C442)) return 442;
            if (typeof(T) == typeof(C443)) return 443;
            if (typeof(T) == typeof(C444)) return 444;
            if (typeof(T) == typeof(C445)) return 445;
            if (typeof(T) == typeof(C446)) return 446;
            if (typeof(T) == typeof(C447)) return 447;
            if (typeof(T) == typeof(C448)) return 448;
            if (typeof(T) == typeof(C449)) return 449;
            if (typeof(T) == typeof(C450)) return 450;
            if (typeof(T) == typeof(C451)) return 451;
            if (typeof(T) == typeof(C452)) return 452;
            if (typeof(T) == typeof(C453)) return 453;
            if (typeof(T) == typeof(C454)) return 454;
            if (typeof(T) == typeof(C455)) return 455;
            if (typeof(T) == typeof(C456)) return 456;
            if (typeof(T) == typeof(C457)) return 457;
            if (typeof(T) == typeof(C458)) return 458;
            if (typeof(T) == typeof(C459)) return 459;
            if (typeof(T) == typeof(C460)) return 460;
            if (typeof(T) == typeof(C461)) return 461;
            if (typeof(T) == typeof(C462)) return 462;
            if (typeof(T) == typeof(C463)) return 463;
            if (typeof(T) == typeof(C464)) return 464;
            if (typeof(T) == typeof(C465)) return 465;
            if (typeof(T) == typeof(C466)) return 466;
            if (typeof(T) == typeof(C467)) return 467;
            if (typeof(T) == typeof(C468)) return 468;
            if (typeof(T) == typeof(C469)) return 469;
            if (typeof(T) == typeof(C470)) return 470;
            if (typeof(T) == typeof(C471)) return 471;
            if (typeof(T) == typeof(C472)) return 472;
            if (typeof(T) == typeof(C473)) return 473;
            if (typeof(T) == typeof(C474)) return 474;
            if (typeof(T) == typeof(C475)) return 475;
            if (typeof(T) == typeof(C476)) return 476;
            if (typeof(T) == typeof(C477)) return 477;
            if (typeof(T) == typeof(C478)) return 478;
            if (typeof(T) == typeof(C479)) return 479;
            if (typeof(T) == typeof(C480)) return 480;
            if (typeof(T) == typeof(C481)) return 481;
            if (typeof(T) == typeof(C482)) return 482;
            if (typeof(T) == typeof(C483)) return 483;
            if (typeof(T) == typeof(C484)) return 484;
            if (typeof(T) == typeof(C485)) return 485;
            if (typeof(T) == typeof(C486)) return 486;
            if (typeof(T) == typeof(C487)) return 487;
            if (typeof(T) == typeof(C488)) return 488;
            if (typeof(T) == typeof(C489)) return 489;
            if (typeof(T) == typeof(C490)) return 490;
            if (typeof(T) == typeof(C491)) return 491;
            if (typeof(T) == typeof(C492)) return 492;
            if (typeof(T) == typeof(C493)) return 493;
            if (typeof(T) == typeof(C494)) return 494;
            if (typeof(T) == typeof(C495)) return 495;
            if (typeof(T) == typeof(C496)) return 496;
            if (typeof(T) == typeof(C497)) return 497;
            if (typeof(T) == typeof(C498)) return 498;
            if (typeof(T) == typeof(C499)) return 499;
            if (typeof(T) == typeof(C500)) return 500;
            if (typeof(T) == typeof(C501)) return 501;
            if (typeof(T) == typeof(C502)) return 502;
            if (typeof(T) == typeof(C503)) return 503;
            if (typeof(T) == typeof(C504)) return 504;
            if (typeof(T) == typeof(C505)) return 505;
            if (typeof(T) == typeof(C506)) return 506;
            if (typeof(T) == typeof(C507)) return 507;
            if (typeof(T) == typeof(C508)) return 508;
            if (typeof(T) == typeof(C509)) return 509;
            if (typeof(T) == typeof(C510)) return 510;
            if (typeof(T) == typeof(C511)) return 511;
            return -1;
        }

        public static int TypeSwitch8(object value) => value switch
        {
            C0 => 0,
            C1 => 1,
            C2 => 2,
            C3 => 3,
            C4 => 4,
            C5 => 5,
            C6 => 6,
            C7 => 7,
            _ => -1,
        };

        public static int TypeSwitch64(object value) => value switch
        {
            C0 => 0,
            C1 => 1,
            C2 => 2,
            C3 => 3,
            C4 => 4,
            C5 => 5,
            C6 => 6,
            C7 => 7,
            C8 => 8,
            C9 => 9,
            C10 => 10,
            C11 => 11,
            C12 => 12,
            C13 => 13,
            C14 => 14,
            C15 => 15,
            C16 => 16,
            C17 => 17,
            C18 => 18,
            C19 => 19,
            C20 => 20,
            C21 => 21,
            C22 => 22,
            C23 => 23,
            C24 => 24,
            C25 => 25,
            C26 => 26,
            C27 => 27,
            C28 => 28,
            C29 => 29,
            C30 => 30,
            C31 => 31,
            C32 => 32,
            C33 => 33,
            C34 => 34,
            C35 => 35,
            C36 => 36,
            C37 => 37,
            C38 => 38,
            C39 => 39,
            C40 => 40,
            C41 => 41,
            C42 => 42,
            C43 => 43,
            C44 => 44,
            C45 => 45,
            C46 => 46,
            C47 => 47,
            C48 => 48,
            C49 => 49,
            C50 => 50,
            C51 => 51,
            C52 => 52,
            C53 => 53,
            C54 => 54,
            C55 => 55,
            C56 => 56,
            C57 => 57,
            C58 => 58,
            C59 => 59,
            C60 => 60,
            C61 => 61,
            C62 => 62,
            C63 => 63,
            _ => -1,
        };

        public static int TypeSwitch512(object value) => value switch
        {
            C0 => 0,
            C1 => 1,
            C2 => 2,
            C3 => 3,
            C4 => 4,
            C5 => 5,
            C6 => 6,
            C7 => 7,
            C8 => 8,
            C9 => 9,
            C10 => 10,
            C11 => 11,
            C12 => 12,
            C13 => 13,
            C14 => 14,
            C15 => 15,
            C16 => 16,
            C17 => 17,
            C18 => 18,
            C19 => 19,
            C20 => 20,
            C21 => 21,
            C22 => 22,
            C23 => 23,
            C24 => 24,
            C25 => 25,
            C26 => 26,
            C27 => 27,
            C28 => 28,
            C29 => 29,
            C30 => 30,
            C31 => 31,
            C32 => 32,
            C33 => 33,
            C34 => 34,
            C35 => 35,
            C36 => 36,
            C37 => 37,
            C38 => 38,
            C39 => 39,
            C40 => 40,
            C41 => 41,
            C42 => 42,
            C43 => 43,
            C44 => 44,
            C45 => 45,
            C46 => 46,
            C47 => 47,
            C48 => 48,
            C49 => 49,
            C50 => 50,
            C51 => 51,
            C52 => 52,
            C53 => 53,
            C54 => 54,
            C55 => 55,
            C56 => 56,
            C57 => 57,
            C58 => 58,
            C59 => 59,
            C60 => 60,
            C61 => 61,
            C62 => 62,
            C63 => 63,
            C64 => 64,
            C65 => 65,
            C66 => 66,
            C67 => 67,
            C68 => 68,
            C69 => 69,
            C70 => 70,
            C71 => 71,
            C72 => 72,
            C73 => 73,
            C74 => 74,
            C75 => 75,
            C76 => 76,
            C77 => 77,
            C78 => 78,
            C79 => 79,
            C80 => 80,
            C81 => 81,
            C82 => 82,
            C83 => 83,
            C84 => 84,
            C85 => 85,
            C86 => 86,
            C87 => 87,
            C88 => 88,
            C89 => 89,
            C90 => 90,
            C91 => 91,
            C92 => 92,
            C93 => 93,
            C94 => 94,
            C95 => 95,
            C96 => 96,
            C97 => 97,
            C98 => 98,
            C99 => 99,
            C100 => 100,
            C101 => 101,
            C102 => 102,
            C103 => 103,
            C104 => 104,
            C105 => 105,
            C106 => 106,
            C107 => 107,
            C108 => 108,
            C109 => 109,
            C110 => 110,
            C111 => 111,
            C112 => 112,
            C113 => 113,
            C114 => 114,
            C115 => 115,
            C116 => 116,
            C117 => 117,
            C118 => 118,
            C119 => 119,
            C120 => 120,
            C121 => 121,
            C122 => 122,
            C123 => 123,
            C124 => 124,
            C125 => 125,
            C126 => 126,
            C127 => 127,
            C128 => 128,
            C129 => 129,
            C130 => 130,
            C131 => 131,
            C132 => 132,
            C133 => 133,
            C134 => 134,
            C135 => 135,
            C136 => 136,
            C137 => 137,
            C138 => 138,
            C139 => 139,
            C140 => 140,
            C141 => 141,
            C142 => 142,
            C143 => 143,
            C144 => 144,
            C145 => 145,
            C146 => 146,
            C147 => 147,
            C148 => 148,
            C149 => 149,
            C150 => 150,
            C151 => 151,
            C152 => 152,
            C153 => 153,
            C154 => 154,
            C155 => 155,
            C156 => 156,
            C157 => 157,
            C158 => 158,
            C159 => 159,
            C160 => 160,
            C161 => 161,
            C162 => 162,
            C163 => 163,
            C164 => 164,
            C165 => 165,
            C166 => 166,
            C167 => 167,
            C168 => 168,
            C169 => 169,
            C170 => 170,
            C171 => 171,
            C172 => 172,
            C173 => 173,
            C174 => 174,
            C175 => 175,
            C176 => 176,
            C177 => 177,
            C178 => 178,
            C179 => 179,
            C180 => 180,
            C181 => 181,
            C182 => 182,
            C183 => 183,
            C184 => 184,
            C185 => 185,
            C186 => 186,
            C187 => 187,
            C188 => 188,
            C189 => 189,
            C190 => 190,
            C191 => 191,
            C192 => 192,
            C193 => 193,
            C194 => 194,
            C195 => 195,
            C196 => 196,
            C197 => 197,
            C198 => 198,
            C199 => 199,
            C200 => 200,
            C201 => 201,
            C202 => 202,
            C203 => 203,
            C204 => 204,
            C205 => 205,
            C206 => 206,
            C207 => 207,
            C208 => 208,
            C209 => 209,
            C210 => 210,
            C211 => 211,
            C212 => 212,
            C213 => 213,
            C214 => 214,
            C215 => 215,
            C216 => 216,
            C217 => 217,
            C218 => 218,
            C219 => 219,
            C220 => 220,
            C221 => 221,
            C222 => 222,
            C223 => 223,
            C224 => 224,
            C225 => 225,
            C226 => 226,
            C227 => 227,
            C228 => 228,
            C229 => 229,
            C230 => 230,
            C231 => 231,
            C232 => 232,
            C233 => 233,
            C234 => 234,
            C235 => 235,
            C236 => 236,
            C237 => 237,
            C238 => 238,
            C239 => 239,
            C240 => 240,
            C241 => 241,
            C242 => 242,
            C243 => 243,
            C244 => 244,
            C245 => 245,
            C246 => 246,
            C247 => 247,
            C248 => 248,
            C249 => 249,
            C250 => 250,
            C251 => 251,
            C252 => 252,
            C253 => 253,
            C254 => 254,
            C255 => 255,
            C256 => 256,
            C257 => 257,
            C258 => 258,
            C259 => 259,
            C260 => 260,
            C261 => 261,
            C262 => 262,
            C263 => 263,
            C264 => 264,
            C265 => 265,
            C266 => 266,
            C267 => 267,
            C268 => 268,
            C269 => 269,
            C270 => 270,
            C271 => 271,
            C272 => 272,
            C273 => 273,
            C274 => 274,
            C275 => 275,
            C276 => 276,
            C277 => 277,
            C278 => 278,
            C279 => 279,
            C280 => 280,
            C281 => 281,
            C282 => 282,
            C283 => 283,
            C284 => 284,
            C285 => 285,
            C286 => 286,
            C287 => 287,
            C288 => 288,
            C289 => 289,
            C290 => 290,
            C291 => 291,
            C292 => 292,
            C293 => 293,
            C294 => 294,
            C295 => 295,
            C296 => 296,
            C297 => 297,
            C298 => 298,
            C299 => 299,
            C300 => 300,
            C301 => 301,
            C302 => 302,
            C303 => 303,
            C304 => 304,
            C305 => 305,
            C306 => 306,
            C307 => 307,
            C308 => 308,
            C309 => 309,
            C310 => 310,
            C311 => 311,
            C312 => 312,
            C313 => 313,
            C314 => 314,
            C315 => 315,
            C316 => 316,
            C317 => 317,
            C318 => 318,
            C319 => 319,
            C320 => 320,
            C321 => 321,
            C322 => 322,
            C323 => 323,
            C324 => 324,
            C325 => 325,
            C326 => 326,
            C327 => 327,
            C328 => 328,
            C329 => 329,
            C330 => 330,
            C331 => 331,
            C332 => 332,
            C333 => 333,
            C334 => 334,
            C335 => 335,
            C336 => 336,
            C337 => 337,
            C338 => 338,
            C339 => 339,
            C340 => 340,
            C341 => 341,
            C342 => 342,
            C343 => 343,
            C344 => 344,
            C345 => 345,
            C346 => 346,
            C347 => 347,
            C348 => 348,
            C349 => 349,
            C350 => 350,
            C351 => 351,
            C352 => 352,
            C353 => 353,
            C354 => 354,
            C355 => 355,
            C356 => 356,
            C357 => 357,
            C358 => 358,
            C359 => 359,
            C360 => 360,
            C361 => 361,
            C362 => 362,
            C363 => 363,
            C364 => 364,
            C365 => 365,
            C366 => 366,
            C367 => 367,
            C368 => 368,
            C369 => 369,
            C370 => 370,
            C371 => 371,
            C372 => 372,
            C373 => 373,
            C374 => 374,
            C375 => 375,
            C376 => 376,
            C377 => 377,
            C378 => 378,
            C379 => 379,
            C380 => 380,
            C381 => 381,
            C382 => 382,
            C383 => 383,
            C384 => 384,
            C385 => 385,
            C386 => 386,
            C387 => 387,
            C388 => 388,
            C389 => 389,
            C390 => 390,
            C391 => 391,
            C392 => 392,
            C393 => 393,
            C394 => 394,
            C395 => 395,
            C396 => 396,
            C397 => 397,
            C398 => 398,
            C399 => 399,
            C400 => 400,
            C401 => 401,
            C402 => 402,
            C403 => 403,
            C404 => 404,
            C405 => 405,
            C406 => 406,
            C407 => 407,
            C408 => 408,
            C409 => 409,
            C410 => 410,
            C411 => 411,
            C412 => 412,
            C413 => 413,
            C414 => 414,
            C415 => 415,
            C416 => 416,
            C417 => 417,
            C418 => 418,
            C419 => 419,
            C420 => 420,
            C421 => 421,
            C422 => 422,
            C423 => 423,
            C424 => 424,
            C425 => 425,
            C426 => 426,
            C427 => 427,
            C428 => 428,
            C429 => 429,
            C430 => 430,
            C431 => 431,
            C432 => 432,
            C433 => 433,
            C434 => 434,
            C435 => 435,
            C436 => 436,
            C437 => 437,
            C438 => 438,
            C439 => 439,
            C440 => 440,
            C441 => 441,
            C442 => 442,
            C443 => 443,
            C444 => 444,
            C445 => 445,
            C446 => 446,
            C447 => 447,
            C448 => 448,
            C449 => 449,
            C450 => 450,
            C451 => 451,
            C452 => 452,
            C453 => 453,
            C454 => 454,
            C455 => 455,
            C456 => 456,
            C457 => 457,
            C458 => 458,
            C459 => 459,
            C460 => 460,
            C461 => 461,
            C462 => 462,
            C463 => 463,
            C464 => 464,
            C465 => 465,
            C466 => 466,
            C467 => 467,
            C468 => 468,
            C469 => 469,
            C470 => 470,
            C471 => 471,
            C472 => 472,
            C473 => 473,
            C474 => 474,
            C475 => 475,
            C476 => 476,
            C477 => 477,
            C478 => 478,
            C479 => 479,
            C480 => 480,
            C481 => 481,
            C482 => 482,
            C483 => 483,
            C484 => 484,
            C485 => 485,
            C486 => 486,
            C487 => 487,
            C488 => 488,
            C489 => 489,
            C490 => 490,
            C491 => 491,
            C492 => 492,
            C493 => 493,
            C494 => 494,
            C495 => 495,
            C496 => 496,
            C497 => 497,
            C498 => 498,
            C499 => 499,
            C500 => 500,
            C501 => 501,
            C502 => 502,
            C503 => 503,
            C504 => 504,
            C505 => 505,
            C506 => 506,
            C507 => 507,
            C508 => 508,
            C509 => 509,
            C510 => 510,
            C511 => 511,
            _ => -1,
        };

        /// <summary>Registers every index into the per-T static, for the Helper strategy.</summary>
        public static void RegisterHelpers()
        {
            Helper<C0>.Index = 0;
            Helper<C1>.Index = 1;
            Helper<C2>.Index = 2;
            Helper<C3>.Index = 3;
            Helper<C4>.Index = 4;
            Helper<C5>.Index = 5;
            Helper<C6>.Index = 6;
            Helper<C7>.Index = 7;
            Helper<C8>.Index = 8;
            Helper<C9>.Index = 9;
            Helper<C10>.Index = 10;
            Helper<C11>.Index = 11;
            Helper<C12>.Index = 12;
            Helper<C13>.Index = 13;
            Helper<C14>.Index = 14;
            Helper<C15>.Index = 15;
            Helper<C16>.Index = 16;
            Helper<C17>.Index = 17;
            Helper<C18>.Index = 18;
            Helper<C19>.Index = 19;
            Helper<C20>.Index = 20;
            Helper<C21>.Index = 21;
            Helper<C22>.Index = 22;
            Helper<C23>.Index = 23;
            Helper<C24>.Index = 24;
            Helper<C25>.Index = 25;
            Helper<C26>.Index = 26;
            Helper<C27>.Index = 27;
            Helper<C28>.Index = 28;
            Helper<C29>.Index = 29;
            Helper<C30>.Index = 30;
            Helper<C31>.Index = 31;
            Helper<C32>.Index = 32;
            Helper<C33>.Index = 33;
            Helper<C34>.Index = 34;
            Helper<C35>.Index = 35;
            Helper<C36>.Index = 36;
            Helper<C37>.Index = 37;
            Helper<C38>.Index = 38;
            Helper<C39>.Index = 39;
            Helper<C40>.Index = 40;
            Helper<C41>.Index = 41;
            Helper<C42>.Index = 42;
            Helper<C43>.Index = 43;
            Helper<C44>.Index = 44;
            Helper<C45>.Index = 45;
            Helper<C46>.Index = 46;
            Helper<C47>.Index = 47;
            Helper<C48>.Index = 48;
            Helper<C49>.Index = 49;
            Helper<C50>.Index = 50;
            Helper<C51>.Index = 51;
            Helper<C52>.Index = 52;
            Helper<C53>.Index = 53;
            Helper<C54>.Index = 54;
            Helper<C55>.Index = 55;
            Helper<C56>.Index = 56;
            Helper<C57>.Index = 57;
            Helper<C58>.Index = 58;
            Helper<C59>.Index = 59;
            Helper<C60>.Index = 60;
            Helper<C61>.Index = 61;
            Helper<C62>.Index = 62;
            Helper<C63>.Index = 63;
            Helper<C64>.Index = 64;
            Helper<C65>.Index = 65;
            Helper<C66>.Index = 66;
            Helper<C67>.Index = 67;
            Helper<C68>.Index = 68;
            Helper<C69>.Index = 69;
            Helper<C70>.Index = 70;
            Helper<C71>.Index = 71;
            Helper<C72>.Index = 72;
            Helper<C73>.Index = 73;
            Helper<C74>.Index = 74;
            Helper<C75>.Index = 75;
            Helper<C76>.Index = 76;
            Helper<C77>.Index = 77;
            Helper<C78>.Index = 78;
            Helper<C79>.Index = 79;
            Helper<C80>.Index = 80;
            Helper<C81>.Index = 81;
            Helper<C82>.Index = 82;
            Helper<C83>.Index = 83;
            Helper<C84>.Index = 84;
            Helper<C85>.Index = 85;
            Helper<C86>.Index = 86;
            Helper<C87>.Index = 87;
            Helper<C88>.Index = 88;
            Helper<C89>.Index = 89;
            Helper<C90>.Index = 90;
            Helper<C91>.Index = 91;
            Helper<C92>.Index = 92;
            Helper<C93>.Index = 93;
            Helper<C94>.Index = 94;
            Helper<C95>.Index = 95;
            Helper<C96>.Index = 96;
            Helper<C97>.Index = 97;
            Helper<C98>.Index = 98;
            Helper<C99>.Index = 99;
            Helper<C100>.Index = 100;
            Helper<C101>.Index = 101;
            Helper<C102>.Index = 102;
            Helper<C103>.Index = 103;
            Helper<C104>.Index = 104;
            Helper<C105>.Index = 105;
            Helper<C106>.Index = 106;
            Helper<C107>.Index = 107;
            Helper<C108>.Index = 108;
            Helper<C109>.Index = 109;
            Helper<C110>.Index = 110;
            Helper<C111>.Index = 111;
            Helper<C112>.Index = 112;
            Helper<C113>.Index = 113;
            Helper<C114>.Index = 114;
            Helper<C115>.Index = 115;
            Helper<C116>.Index = 116;
            Helper<C117>.Index = 117;
            Helper<C118>.Index = 118;
            Helper<C119>.Index = 119;
            Helper<C120>.Index = 120;
            Helper<C121>.Index = 121;
            Helper<C122>.Index = 122;
            Helper<C123>.Index = 123;
            Helper<C124>.Index = 124;
            Helper<C125>.Index = 125;
            Helper<C126>.Index = 126;
            Helper<C127>.Index = 127;
            Helper<C128>.Index = 128;
            Helper<C129>.Index = 129;
            Helper<C130>.Index = 130;
            Helper<C131>.Index = 131;
            Helper<C132>.Index = 132;
            Helper<C133>.Index = 133;
            Helper<C134>.Index = 134;
            Helper<C135>.Index = 135;
            Helper<C136>.Index = 136;
            Helper<C137>.Index = 137;
            Helper<C138>.Index = 138;
            Helper<C139>.Index = 139;
            Helper<C140>.Index = 140;
            Helper<C141>.Index = 141;
            Helper<C142>.Index = 142;
            Helper<C143>.Index = 143;
            Helper<C144>.Index = 144;
            Helper<C145>.Index = 145;
            Helper<C146>.Index = 146;
            Helper<C147>.Index = 147;
            Helper<C148>.Index = 148;
            Helper<C149>.Index = 149;
            Helper<C150>.Index = 150;
            Helper<C151>.Index = 151;
            Helper<C152>.Index = 152;
            Helper<C153>.Index = 153;
            Helper<C154>.Index = 154;
            Helper<C155>.Index = 155;
            Helper<C156>.Index = 156;
            Helper<C157>.Index = 157;
            Helper<C158>.Index = 158;
            Helper<C159>.Index = 159;
            Helper<C160>.Index = 160;
            Helper<C161>.Index = 161;
            Helper<C162>.Index = 162;
            Helper<C163>.Index = 163;
            Helper<C164>.Index = 164;
            Helper<C165>.Index = 165;
            Helper<C166>.Index = 166;
            Helper<C167>.Index = 167;
            Helper<C168>.Index = 168;
            Helper<C169>.Index = 169;
            Helper<C170>.Index = 170;
            Helper<C171>.Index = 171;
            Helper<C172>.Index = 172;
            Helper<C173>.Index = 173;
            Helper<C174>.Index = 174;
            Helper<C175>.Index = 175;
            Helper<C176>.Index = 176;
            Helper<C177>.Index = 177;
            Helper<C178>.Index = 178;
            Helper<C179>.Index = 179;
            Helper<C180>.Index = 180;
            Helper<C181>.Index = 181;
            Helper<C182>.Index = 182;
            Helper<C183>.Index = 183;
            Helper<C184>.Index = 184;
            Helper<C185>.Index = 185;
            Helper<C186>.Index = 186;
            Helper<C187>.Index = 187;
            Helper<C188>.Index = 188;
            Helper<C189>.Index = 189;
            Helper<C190>.Index = 190;
            Helper<C191>.Index = 191;
            Helper<C192>.Index = 192;
            Helper<C193>.Index = 193;
            Helper<C194>.Index = 194;
            Helper<C195>.Index = 195;
            Helper<C196>.Index = 196;
            Helper<C197>.Index = 197;
            Helper<C198>.Index = 198;
            Helper<C199>.Index = 199;
            Helper<C200>.Index = 200;
            Helper<C201>.Index = 201;
            Helper<C202>.Index = 202;
            Helper<C203>.Index = 203;
            Helper<C204>.Index = 204;
            Helper<C205>.Index = 205;
            Helper<C206>.Index = 206;
            Helper<C207>.Index = 207;
            Helper<C208>.Index = 208;
            Helper<C209>.Index = 209;
            Helper<C210>.Index = 210;
            Helper<C211>.Index = 211;
            Helper<C212>.Index = 212;
            Helper<C213>.Index = 213;
            Helper<C214>.Index = 214;
            Helper<C215>.Index = 215;
            Helper<C216>.Index = 216;
            Helper<C217>.Index = 217;
            Helper<C218>.Index = 218;
            Helper<C219>.Index = 219;
            Helper<C220>.Index = 220;
            Helper<C221>.Index = 221;
            Helper<C222>.Index = 222;
            Helper<C223>.Index = 223;
            Helper<C224>.Index = 224;
            Helper<C225>.Index = 225;
            Helper<C226>.Index = 226;
            Helper<C227>.Index = 227;
            Helper<C228>.Index = 228;
            Helper<C229>.Index = 229;
            Helper<C230>.Index = 230;
            Helper<C231>.Index = 231;
            Helper<C232>.Index = 232;
            Helper<C233>.Index = 233;
            Helper<C234>.Index = 234;
            Helper<C235>.Index = 235;
            Helper<C236>.Index = 236;
            Helper<C237>.Index = 237;
            Helper<C238>.Index = 238;
            Helper<C239>.Index = 239;
            Helper<C240>.Index = 240;
            Helper<C241>.Index = 241;
            Helper<C242>.Index = 242;
            Helper<C243>.Index = 243;
            Helper<C244>.Index = 244;
            Helper<C245>.Index = 245;
            Helper<C246>.Index = 246;
            Helper<C247>.Index = 247;
            Helper<C248>.Index = 248;
            Helper<C249>.Index = 249;
            Helper<C250>.Index = 250;
            Helper<C251>.Index = 251;
            Helper<C252>.Index = 252;
            Helper<C253>.Index = 253;
            Helper<C254>.Index = 254;
            Helper<C255>.Index = 255;
            Helper<C256>.Index = 256;
            Helper<C257>.Index = 257;
            Helper<C258>.Index = 258;
            Helper<C259>.Index = 259;
            Helper<C260>.Index = 260;
            Helper<C261>.Index = 261;
            Helper<C262>.Index = 262;
            Helper<C263>.Index = 263;
            Helper<C264>.Index = 264;
            Helper<C265>.Index = 265;
            Helper<C266>.Index = 266;
            Helper<C267>.Index = 267;
            Helper<C268>.Index = 268;
            Helper<C269>.Index = 269;
            Helper<C270>.Index = 270;
            Helper<C271>.Index = 271;
            Helper<C272>.Index = 272;
            Helper<C273>.Index = 273;
            Helper<C274>.Index = 274;
            Helper<C275>.Index = 275;
            Helper<C276>.Index = 276;
            Helper<C277>.Index = 277;
            Helper<C278>.Index = 278;
            Helper<C279>.Index = 279;
            Helper<C280>.Index = 280;
            Helper<C281>.Index = 281;
            Helper<C282>.Index = 282;
            Helper<C283>.Index = 283;
            Helper<C284>.Index = 284;
            Helper<C285>.Index = 285;
            Helper<C286>.Index = 286;
            Helper<C287>.Index = 287;
            Helper<C288>.Index = 288;
            Helper<C289>.Index = 289;
            Helper<C290>.Index = 290;
            Helper<C291>.Index = 291;
            Helper<C292>.Index = 292;
            Helper<C293>.Index = 293;
            Helper<C294>.Index = 294;
            Helper<C295>.Index = 295;
            Helper<C296>.Index = 296;
            Helper<C297>.Index = 297;
            Helper<C298>.Index = 298;
            Helper<C299>.Index = 299;
            Helper<C300>.Index = 300;
            Helper<C301>.Index = 301;
            Helper<C302>.Index = 302;
            Helper<C303>.Index = 303;
            Helper<C304>.Index = 304;
            Helper<C305>.Index = 305;
            Helper<C306>.Index = 306;
            Helper<C307>.Index = 307;
            Helper<C308>.Index = 308;
            Helper<C309>.Index = 309;
            Helper<C310>.Index = 310;
            Helper<C311>.Index = 311;
            Helper<C312>.Index = 312;
            Helper<C313>.Index = 313;
            Helper<C314>.Index = 314;
            Helper<C315>.Index = 315;
            Helper<C316>.Index = 316;
            Helper<C317>.Index = 317;
            Helper<C318>.Index = 318;
            Helper<C319>.Index = 319;
            Helper<C320>.Index = 320;
            Helper<C321>.Index = 321;
            Helper<C322>.Index = 322;
            Helper<C323>.Index = 323;
            Helper<C324>.Index = 324;
            Helper<C325>.Index = 325;
            Helper<C326>.Index = 326;
            Helper<C327>.Index = 327;
            Helper<C328>.Index = 328;
            Helper<C329>.Index = 329;
            Helper<C330>.Index = 330;
            Helper<C331>.Index = 331;
            Helper<C332>.Index = 332;
            Helper<C333>.Index = 333;
            Helper<C334>.Index = 334;
            Helper<C335>.Index = 335;
            Helper<C336>.Index = 336;
            Helper<C337>.Index = 337;
            Helper<C338>.Index = 338;
            Helper<C339>.Index = 339;
            Helper<C340>.Index = 340;
            Helper<C341>.Index = 341;
            Helper<C342>.Index = 342;
            Helper<C343>.Index = 343;
            Helper<C344>.Index = 344;
            Helper<C345>.Index = 345;
            Helper<C346>.Index = 346;
            Helper<C347>.Index = 347;
            Helper<C348>.Index = 348;
            Helper<C349>.Index = 349;
            Helper<C350>.Index = 350;
            Helper<C351>.Index = 351;
            Helper<C352>.Index = 352;
            Helper<C353>.Index = 353;
            Helper<C354>.Index = 354;
            Helper<C355>.Index = 355;
            Helper<C356>.Index = 356;
            Helper<C357>.Index = 357;
            Helper<C358>.Index = 358;
            Helper<C359>.Index = 359;
            Helper<C360>.Index = 360;
            Helper<C361>.Index = 361;
            Helper<C362>.Index = 362;
            Helper<C363>.Index = 363;
            Helper<C364>.Index = 364;
            Helper<C365>.Index = 365;
            Helper<C366>.Index = 366;
            Helper<C367>.Index = 367;
            Helper<C368>.Index = 368;
            Helper<C369>.Index = 369;
            Helper<C370>.Index = 370;
            Helper<C371>.Index = 371;
            Helper<C372>.Index = 372;
            Helper<C373>.Index = 373;
            Helper<C374>.Index = 374;
            Helper<C375>.Index = 375;
            Helper<C376>.Index = 376;
            Helper<C377>.Index = 377;
            Helper<C378>.Index = 378;
            Helper<C379>.Index = 379;
            Helper<C380>.Index = 380;
            Helper<C381>.Index = 381;
            Helper<C382>.Index = 382;
            Helper<C383>.Index = 383;
            Helper<C384>.Index = 384;
            Helper<C385>.Index = 385;
            Helper<C386>.Index = 386;
            Helper<C387>.Index = 387;
            Helper<C388>.Index = 388;
            Helper<C389>.Index = 389;
            Helper<C390>.Index = 390;
            Helper<C391>.Index = 391;
            Helper<C392>.Index = 392;
            Helper<C393>.Index = 393;
            Helper<C394>.Index = 394;
            Helper<C395>.Index = 395;
            Helper<C396>.Index = 396;
            Helper<C397>.Index = 397;
            Helper<C398>.Index = 398;
            Helper<C399>.Index = 399;
            Helper<C400>.Index = 400;
            Helper<C401>.Index = 401;
            Helper<C402>.Index = 402;
            Helper<C403>.Index = 403;
            Helper<C404>.Index = 404;
            Helper<C405>.Index = 405;
            Helper<C406>.Index = 406;
            Helper<C407>.Index = 407;
            Helper<C408>.Index = 408;
            Helper<C409>.Index = 409;
            Helper<C410>.Index = 410;
            Helper<C411>.Index = 411;
            Helper<C412>.Index = 412;
            Helper<C413>.Index = 413;
            Helper<C414>.Index = 414;
            Helper<C415>.Index = 415;
            Helper<C416>.Index = 416;
            Helper<C417>.Index = 417;
            Helper<C418>.Index = 418;
            Helper<C419>.Index = 419;
            Helper<C420>.Index = 420;
            Helper<C421>.Index = 421;
            Helper<C422>.Index = 422;
            Helper<C423>.Index = 423;
            Helper<C424>.Index = 424;
            Helper<C425>.Index = 425;
            Helper<C426>.Index = 426;
            Helper<C427>.Index = 427;
            Helper<C428>.Index = 428;
            Helper<C429>.Index = 429;
            Helper<C430>.Index = 430;
            Helper<C431>.Index = 431;
            Helper<C432>.Index = 432;
            Helper<C433>.Index = 433;
            Helper<C434>.Index = 434;
            Helper<C435>.Index = 435;
            Helper<C436>.Index = 436;
            Helper<C437>.Index = 437;
            Helper<C438>.Index = 438;
            Helper<C439>.Index = 439;
            Helper<C440>.Index = 440;
            Helper<C441>.Index = 441;
            Helper<C442>.Index = 442;
            Helper<C443>.Index = 443;
            Helper<C444>.Index = 444;
            Helper<C445>.Index = 445;
            Helper<C446>.Index = 446;
            Helper<C447>.Index = 447;
            Helper<C448>.Index = 448;
            Helper<C449>.Index = 449;
            Helper<C450>.Index = 450;
            Helper<C451>.Index = 451;
            Helper<C452>.Index = 452;
            Helper<C453>.Index = 453;
            Helper<C454>.Index = 454;
            Helper<C455>.Index = 455;
            Helper<C456>.Index = 456;
            Helper<C457>.Index = 457;
            Helper<C458>.Index = 458;
            Helper<C459>.Index = 459;
            Helper<C460>.Index = 460;
            Helper<C461>.Index = 461;
            Helper<C462>.Index = 462;
            Helper<C463>.Index = 463;
            Helper<C464>.Index = 464;
            Helper<C465>.Index = 465;
            Helper<C466>.Index = 466;
            Helper<C467>.Index = 467;
            Helper<C468>.Index = 468;
            Helper<C469>.Index = 469;
            Helper<C470>.Index = 470;
            Helper<C471>.Index = 471;
            Helper<C472>.Index = 472;
            Helper<C473>.Index = 473;
            Helper<C474>.Index = 474;
            Helper<C475>.Index = 475;
            Helper<C476>.Index = 476;
            Helper<C477>.Index = 477;
            Helper<C478>.Index = 478;
            Helper<C479>.Index = 479;
            Helper<C480>.Index = 480;
            Helper<C481>.Index = 481;
            Helper<C482>.Index = 482;
            Helper<C483>.Index = 483;
            Helper<C484>.Index = 484;
            Helper<C485>.Index = 485;
            Helper<C486>.Index = 486;
            Helper<C487>.Index = 487;
            Helper<C488>.Index = 488;
            Helper<C489>.Index = 489;
            Helper<C490>.Index = 490;
            Helper<C491>.Index = 491;
            Helper<C492>.Index = 492;
            Helper<C493>.Index = 493;
            Helper<C494>.Index = 494;
            Helper<C495>.Index = 495;
            Helper<C496>.Index = 496;
            Helper<C497>.Index = 497;
            Helper<C498>.Index = 498;
            Helper<C499>.Index = 499;
            Helper<C500>.Index = 500;
            Helper<C501>.Index = 501;
            Helper<C502>.Index = 502;
            Helper<C503>.Index = 503;
            Helper<C504>.Index = 504;
            Helper<C505>.Index = 505;
            Helper<C506>.Index = 506;
            Helper<C507>.Index = 507;
            Helper<C508>.Index = 508;
            Helper<C509>.Index = 509;
            Helper<C510>.Index = 510;
            Helper<C511>.Index = 511;
        }
    }
}
#endif
