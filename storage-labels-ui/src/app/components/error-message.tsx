import { FormHelperText } from '@mui/material';
import React from 'react';
import { StandardValidationResult } from '../../utils/validate';


export const ErrorMessage: React.FunctionComponent<{ isSubmitted: boolean, inputName: string, results: StandardValidationResult[] }> = ({ isSubmitted, inputName, results }) => {

    const errors = results
        .filter(result => inputName === result.name)
        .map(({message}) => message);

    if (errors.length > 0 && isSubmitted) {
        return (
            <FormHelperText error component="div">
                {
                    errors.map((error, idx) => <div key={`error_${idx}`}>{error}</div>)
                }
            </FormHelperText>
        );
    }
    else {
        return null;
    }
}