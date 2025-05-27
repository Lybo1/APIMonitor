// #include "../../include/helpers/grading.h"
//
// #include <stdio.h>
// #include <string.h>
//
// static header_rule rules[] = {
//     {"Strict-Transport-Security", 30, 1},
//     {"Content-Security-Policy", 30, 1},
//     {"X-Frame-Options", 20, 1},
//     {"X-Content-Type-Options", 10, 1},
//     {"Referrer-Policy", 10, 1},
//     {"Feature-Policy", 5, 0},
//     {"Server", 0, -1}
// };
//
// #define RULES_COUNT (sizeof(rules) / sizeof(rules[0]))
// #define MAX_SCORE 100
//
// static int header_present(const cJSON* header_json, const char* header_name) {
//     const cJSON* item = cJSON_GetObjectItemCaseSensitive((cJSON*)header_json, header_name);
//
//     return (item != NULL);
// }
//
// grading_result grading_analyze(const cJSON* headers_json) {
//     grading_result result = {0};
//     result.score = MAX_SCORE;
//     result.missing = cJSON_CreateArray();
//     result.notes = cJSON_CreateArray();
//
//     for (size_t i = 0; i < RULES_COUNT; i++) {
//         const header_rule* rule = &rules[i];
//         int present = header_present(headers_json, rule->header_name);
//
//         if (rule->must_exist == 1 && !present) {
//             result.score -= rule->weight;
//
//             cJSON_AddItemToArray(result.missing, cJSON_CreateString(rule->header_name));
//
//             char note_buf[256];
//             snprintf(note_buf, sizeof(note_buf), "Missing important security header: %s", rule->header_name);
//             cJSON_AddItemToArray(result.notes, cJSON_CreateString(note_buf));
//         }
//         else if (rule->must_exist == -1 && present) {
//             result.score -= rule->weight;
//
//             cJSON_AddItemToArray(result.missing, cJSON_CreateString(rule->header_name));
//             cJSON_AddItemToArray(result.notes, cJSON_CreateString("Header should be absent for security reasons"));
//         }
//         else if (rule->must_exist == 0 && !present) {
//             result.score -= rule->weight;
//         }
//     }
//
//     if (result.score < 0) {
//         result.score = 0;
//     }
//
//     if (result.score > MAX_SCORE) {
//         result.score = MAX_SCORE;
//     }
//
//     return result;
// }
//
// void grading_result_free(grading_result* result) {
//     if (!result) {
//         return;
//     }
//
//     if (result->missing) {
//         cJSON_Delete(result->missing);
//     }
//
//     if (result->notes) {
//         cJSON_Delete(result->notes);
//     }
// }