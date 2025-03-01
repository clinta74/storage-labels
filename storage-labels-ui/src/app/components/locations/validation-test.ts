import { ValidationTest } from "../../../utils/validate";

export const validationTests: ValidationTest<StorageLocation>[] =
    [
        {
            passCondition: ({ name }) => name.trim().length > 0,
            result: {
                message: 'There must be a name.',
                name: 'name',
            }
        }
    ];