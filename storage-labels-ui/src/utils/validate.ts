export interface StandardValidationResult {
    message: string;
    name: string;
}
export interface ValidationTest<P, M = StandardValidationResult> { 
    passCondition: (params: P) => boolean, result: M 
}

export interface ValidationResult<M> {
    valid: boolean,
    results: M[] | [],
}

export type SimpleValidationTest<M> = ValidationTest<string, {item: M, items: M[]}>;

const sumValidationResult = <M>(result1: ValidationResult<M>, result2: ValidationResult<M>) => {
    const valid = result1.valid && result2.valid;
    return ({
        valid,
        results: result2.valid ? result1.results : [...result1.results, ...result2.results],
    });
};

const validate = <P, M>(test: ValidationTest<P, M>, params: P): ValidationResult<M>  => {
    return ({
        valid: test.passCondition(params),
        results: [test.result],
    });
};

const getValidResult = <M>() => {
    return ({
        valid: true,
        results: new Array<M>(),
    });
};

const validateAll = <M, P>(tests: ValidationTest<P, M>[], params: P):[ valid: boolean, results: M[]] => {
    const testResults = tests.reduce((prev, next) => {
        return sumValidationResult<M>(prev as ValidationResult<M>, validate<P, M>(next, params))
    }, getValidResult<M>());

    return [ testResults.valid, testResults.results ];
};

export {
    validate,
    validateAll
};