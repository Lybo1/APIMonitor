#ifndef GRADING_H
#define GRADING_H

#include "cjson/cJSON.h"

typedef struct {
  const char *header_name;
  int weight;
  int must_exist;
} header_rule;

typedef struct {
  int score;
  cJSON* missing;
  cJSON* notes;
} grading_result;

grading_result grading_analyze(const cJSON* headers_json);

void grading_result_free(grading_result *result);

#endif
